using DrMAdWill.Core.BaseModels;

namespace DrMAdWill.Core.Abstractions;

public interface IEventBus
{
    void Publish(IntegrationEvent @event);

    void Subscribe<T, TH>()
       where T : IntegrationEvent
       where TH : IIntegrationEventHandler<T>;

    void UnSubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;
}