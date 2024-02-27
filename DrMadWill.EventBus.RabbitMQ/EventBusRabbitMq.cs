using System.Net.Sockets;
using System.Text;
using DrMadWill.EventBus.Base;
using DrMadWill.EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace DrMadWill.EventBus.RabbitMQ;

public class EventBusRabbitMq : BaseEventBus
{
    private readonly RabbitMqPersistentConnection _persistentConnection;
    private readonly IModel _consumerChannel;
    public EventBusRabbitMq(EventBusConfig config, IServiceProvider serviceProvider, IConnectionFactory connectionFactory) : base(config, serviceProvider)
    {
        _persistentConnection = new RabbitMqPersistentConnection(connectionFactory,config.ConnectionRetryCount);
        _consumerChannel = CreateConsumerChannel();
        SubManager.OnEventRemoved += Submanger_OnEventRemoved;
    }

    private void Submanger_OnEventRemoved(object? sender, string eventName)
    {
        eventName = ProcessEventName(eventName);
        TryConnect();

        _consumerChannel.QueueUnbind(queue:eventName,exchange:EventBusConfig.DefaultTopicName,routingKey:eventName);

        if (SubManager.IsEmpty)
        {
            _consumerChannel.Close();
        }

    }

    public override void Publish(IntegrationEvent @event)
    {
        TryConnect();

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(EventBusConfig.ConnectionRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) =>
                {
                    Console.WriteLine("ex => Event Bus Publish In RabbitMQ =>  " + ex);
                });
        var eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);
        
        _consumerChannel.ExchangeDeclare(exchange:EventBusConfig.DefaultTopicName,type:"direct");
       
        
        
        var message = JsonConvert.SerializeObject(@event);
        var body = Encoding.UTF8.GetBytes(message);
        policy.Execute(() =>
        {

            var props = _consumerChannel.CreateBasicProperties();
            props.DeliveryMode = 2;
            
            // _consumerChannel.QueueDeclare(queue: GetSubName(eventName),
            //     durable: true,
            //     exclusive: false,
            //     autoDelete: false,
            //     arguments: null);
            //
            // _consumerChannel.QueueBind(queue: GetSubName(eventName), exchange: EventBusConfig.DefaultTopicName,
            //     routingKey: eventName);
            
            _consumerChannel.BasicPublish(exchange: EventBusConfig.DefaultTopicName, routingKey: eventName,
                mandatory: true, basicProperties: props, body: body);
        }); 

    }

    public override void Subscribe<T, TH>()
    {
        var eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);
        if (!SubManager.HasSubscriptionForEvent(eventName))
        {
            if (!_persistentConnection.IsConnection)
            {
                _persistentConnection.TryConnect();
            }

            _consumerChannel.QueueDeclare(queue: GetSubName(eventName),// ensure queue with consuming
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _consumerChannel.QueueBind(queue: GetSubName(eventName), exchange: EventBusConfig.DefaultTopicName,
                routingKey: eventName);
            
        }
        SubManager.AddSubscription<T,TH>();  
        StartBasicConsume(eventName);
    }

    private void StartBasicConsume(string eventName)
    {
        if (_consumerChannel != null)
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += Consumer_Received;

            _consumerChannel.BasicConsume(queue: GetSubName(eventName), autoAck: false, consumer: consumer);
        }

    }

    private async  void Consumer_Received(object? sender, BasicDeliverEventArgs e)
    {
        var eventName = e.RoutingKey;
        eventName = ProcessEventName(eventName);
        var message = Encoding.UTF8.GetString(e.Body.Span);

        try
        {
            await ProcessEvent(eventName, message);
        }
        catch (Exception exception)
        {
            // logging
            
        } 
        _consumerChannel.BasicAck(e.DeliveryTag,multiple:false);

        
    }

    public override void UnSubscribe<T, TH>()
    {
        SubManager.RemoveSubscription<T,TH>();
    }

    private IModel CreateConsumerChannel()
    {
        TryConnect();

        var channel = _persistentConnection.CreateModel();
        channel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName,type :"direct");

        return channel;
    }
    
    private void TryConnect()
    {
        if (!_persistentConnection.IsConnection)
        {
            _persistentConnection.TryConnect();
        } 
    }

}