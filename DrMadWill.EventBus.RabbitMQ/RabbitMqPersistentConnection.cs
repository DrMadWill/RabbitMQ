using System.Net.Sockets;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace DrMadWill.EventBus.RabbitMQ;

public class RabbitMqPersistentConnection : IDisposable
{
    private IConnection _connection;
    private readonly IConnectionFactory _connectionFactory;
    private readonly int _tryCount;
    private readonly object _lockObject = new object();
    private bool _isDisposed = false;
    public RabbitMqPersistentConnection(IConnectionFactory connectionFactory,int tryCount = 5)
    {
        _connectionFactory = connectionFactory;
        _tryCount = tryCount;
    }

    public bool IsConnection => _connection != null && _connection.IsOpen;

    public IModel CreateModel()
    {
        return _connection.CreateModel();
    }

    public void Dispose()
    {
        _isDisposed = true;
        _connection.Dispose();
    }

    public bool TryConnect()
    {

        lock (_lockObject)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_tryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) =>
                    {
                        Console.WriteLine("ex => Event Bus TryConnect In RabbitMQ =>  " + ex);
                    });

            policy.Execute(() =>
            {
                _connection = _connectionFactory.CreateConnection();
            });

            if (IsConnection)
            {
                _connection.ConnectionShutdown += Connection_ConnectionShutdown; 
                _connection.CallbackException += Connection_CallbackException; 
                _connection.ConnectionBlocked += Connection_ConnectionBlocked; 
                
                return true;
            }

            return false;

        }
    }

    private void Connection_ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if(_isDisposed) return;
        TryConnect();
    }

    private void Connection_CallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if(_isDisposed) return;
        TryConnect();
    }

    private void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if(_isDisposed) return;
        TryConnect();
    }
}