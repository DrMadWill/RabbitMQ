using DrMAdWill.Core.Abstractions;
using DrMAdWill.Core.BaseModels;
using DrMadWill.EventBus.Base.Events;

namespace DrMadWill.EventBus.Base.Abstractions;

public interface IEventBusSubscriptionManager
{
    bool IsEmpty { get; }

    event EventHandler<string> OnEventRemoved;

    void AddSubscription<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;

    void RemoveSubscription<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;

    bool HasSubscriptionForEvent<T>() where T : IntegrationEvent;

    bool HasSubscriptionForEvent(string eventName);

    Type GetEventTypeByName(string eventName);

    void Clear();

    IEnumerable<SubscriptionInfo> GetHandlerForEvent<T>() where T : IntegrationEvent;

    IEnumerable<SubscriptionInfo> GetHandlerForEvent(string name);

    string GetEventKey<T>();
}