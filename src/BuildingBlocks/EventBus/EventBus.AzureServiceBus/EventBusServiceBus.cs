using EventBus.Base;
using EventBus.Base.Events;

namespace EventBus.AzureServiceBus;

public class EventBusServiceBus : BaseEventBus
{
    public EventBusServiceBus(IServiceProvider serviceProvider, EventBusConfig eventBusConfig) : base(serviceProvider, eventBusConfig)
    {
    }

    public override void Publish(IntegrationEvent @event)
    {
        throw new NotImplementedException();
    }

    public override void Subscribe<T, THandler>()
    {
        throw new NotImplementedException();
    }

    public override void UnSubscribe<T, THandler>()
    {
        throw new NotImplementedException();
    }
}