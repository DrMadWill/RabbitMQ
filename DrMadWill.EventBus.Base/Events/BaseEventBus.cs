using DrMadWill.EventBus.Base.Abstractions;
using DrMadWill.EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace DrMadWill.EventBus.Base.Events;

public abstract class BaseEventBus : IEventBus
{
    public readonly IServiceProvider ServiceProvider;
    public readonly IEventBusSubscriptionManager SubManager;
    public EventBusConfig EventBusConfig;

    public BaseEventBus(EventBusConfig config, IServiceProvider serviceProvider)
    {
        EventBusConfig = config;
        ServiceProvider = serviceProvider;
        SubManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
    }

    public virtual string ProcessEventName(string eventName)
    {
        if (EventBusConfig.DeleteEventPrefix && eventName.Contains(EventBusConfig.EventNamePrefix))
            eventName = eventName[(EventBusConfig.EventNamePrefix.Length - 1)..];
        if (EventBusConfig.DeleteEventSuffix && eventName.Contains(EventBusConfig.EventNameSuffix))
            eventName = eventName[..eventName.IndexOf(EventBusConfig.EventNameSuffix, StringComparison.Ordinal)];
        if (eventName.Contains(EventBusConfig.SubscriberClientAppName))
            eventName = eventName.Replace(EventBusConfig.SubscriberClientAppName + ".", "");
        return eventName;
    }

    public virtual string GetSubName(string eventName)
    {
        return $"{EventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
    }

    public virtual void Dispose()
    {
        EventBusConfig = null;
    }

    public async Task<bool> ProcessEvent(string eventName, string message)
    {
        eventName = ProcessEventName(eventName);
        var processed = false;
        if (!SubManager.HasSubscriptionForEvent(eventName)) return processed;
        var subscriptions = SubManager.GetHandlerForEvent(eventName);
        using (var scope = ServiceProvider.CreateScope())
        {
            foreach (var subscription in subscriptions)
            {
                try
                {
                    var handler = ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null) continue;
                    var eventType =
                        SubManager.GetEventTypeByName(
                            $"{EventBusConfig.EventNamePrefix}{eventName}{EventBusConfig.EventNameSuffix}");
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    
                }
            }
        }

        processed = true;
        return processed;
    }

    public abstract void Publish(IntegrationEvent @event);

    public abstract void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

    public abstract void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
}