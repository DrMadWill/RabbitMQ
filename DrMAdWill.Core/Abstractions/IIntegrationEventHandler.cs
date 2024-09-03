using DrMAdWill.Core.BaseModels;

namespace DrMAdWill.Core.Abstractions;

// ReSharper disable once InconsistentNaming
public interface IIntegrationEventHandler<TIntegrationEvent> : IntegrationEventHandler
where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
}

public interface IntegrationEventHandler
{
}