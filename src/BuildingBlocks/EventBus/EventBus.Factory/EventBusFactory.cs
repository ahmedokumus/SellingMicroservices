using EventBus.AzureServiceBus;
using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.RabbitMQ;

namespace EventBus.Factory;

public static class EventBusFactory
{
    public static IEventBus Create(EventBusConfig config, IServiceProvider serviceProvider)
    {
        switch (config.EventBusType)
        {
            case EventBusType.RabbitMq:
                return new EventBusRabbitMq(serviceProvider, config);
            case EventBusType.AzureServiceBus:
                return new EventBusServiceBus(serviceProvider, config);
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        //Aşağıdaki kod bloğu ile yukarıdaki kod bloğu aynı işi yapıyor.

        //return config.EventBusType switch
        //{
        //    EventBusType.AzureServiceBus => new EventBusServiceBus(serviceProvider, config),
        //    _ => new EventBusRabbitMq(serviceProvider, config),
        //};
    }
}