using DrMadWill.EventBus.Base.Events;

namespace DrMadWill.EventBus.Base.Abstractions;

// ReSharper disable once InconsistentNaming
public interface IIntegrationEventHandler<IIntegrationEvent> : IntegrationEventHandler
where IIntegrationEvent : IntegrationEvent
{
    Task Handle(IIntegrationEvent @event);
}

public interface IntegrationEventHandler
{
}