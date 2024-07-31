using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events;

public abstract class BaseEventBus : IEventBus
{
    public readonly IServiceProvider ServiceProvider;
    public readonly IEventBusSubscriptionManager SubscriptionManager;
    private EventBusConfig _eventBusConfig;

    protected BaseEventBus(IServiceProvider serviceProvider, EventBusConfig eventBusConfig)
    {
        ServiceProvider = serviceProvider;
        SubscriptionManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
        _eventBusConfig = eventBusConfig;
    }

    public virtual string ProcessEventName(string eventName)
    {
        if (_eventBusConfig.DeleteEventPrefix)
        {
            return eventName.TrimStart(_eventBusConfig.EventNamePrefix.ToArray());
        }

        if (_eventBusConfig.DeleteEventSuffix)
            return eventName.TrimEnd(_eventBusConfig.EventNameSuffix.ToArray());

        return eventName;
    }

    public virtual string GetSubName(string eventName)
    {
        return $"{_eventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
    }

    public virtual void Dispose()
    {
        _eventBusConfig = null!;
    }

    public async Task<bool> ProcessEvent(string eventName, string message)
    {
        eventName = ProcessEventName(eventName);

        var processed = false;

        if (SubscriptionManager.HasSubscriptionForEvent(eventName))
        {
            var subscriptions = SubscriptionManager.GetHandlersForEvent(eventName);

            using (ServiceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null) 
                        continue;

                    var eventType = SubscriptionManager.GetEventTypeByName(
                        $"{_eventBusConfig.EventNamePrefix}{eventName}{_eventBusConfig.EventNameSuffix}");
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType.GetMethod("Handle")!.Invoke(handler, new[] { integrationEvent })!;
                }
            }
            processed = true;
        }

        return processed;
    }

    public abstract void Publish(IntegrationEvent @event);

    public abstract void Subscribe<T, THandler>() where T : IntegrationEvent where THandler : IIntegrationEventHandler<T>;

    public abstract void UnSubscribe<T, THandler>() where T : IntegrationEvent where THandler : IIntegrationEventHandler<T>;
}