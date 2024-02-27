using DrMadWill.EventBus.Base.Events;

namespace DrMadWill.EventBus.Base.Abstractions;

// ReSharper disable once InconsistentNaming
public interface IIntegrationEventHandler<TIntegrationEvent> : IntegrationEventHandler
where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
}

public interface IntegrationEventHandler
{
}