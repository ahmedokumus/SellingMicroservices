using EventBus.Base.Abstraction;
using EventBus.Base.Events;

namespace EventBus.Base.SubManagers;

public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
    private readonly List<Type> _eventTypes;
    private readonly Func<string, string> _eventNameGetter;

    public InMemoryEventBusSubscriptionManager(Func<string, string> eventNameGetter)
    {
        _handlers = new Dictionary<string, List<SubscriptionInfo>>();
        _eventTypes = new List<Type>();
        _eventNameGetter = eventNameGetter;
    }

    public bool IsEmpty => _handlers.Keys.Any() == false;

    public event EventHandler<string>? OnEventRemoved;

    public void AddSubscription<T, THandler>() where T : IntegrationEvent where THandler : IIntegrationEventHandler<T>
    {
        string eventName = GetEventKey<T>();

        AddHandler(typeof(THandler), eventName);

        if (_eventTypes.Contains(typeof(T)) == false)
        {
            _eventTypes.Add(typeof(T));
        }
    }

    private void AddHandler(Type handlerType, string eventName)
    {
        if (HasSubscriptionForEvent(eventName) == false)
        {
            _handlers.Add(eventName, new List<SubscriptionInfo>());
        }

        if (_handlers[eventName].Any(x => x.HandlerType == handlerType))
        {
            throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'",
                nameof(handlerType));
        }

        _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
    }

    public void RemoveSubscription<T, THandler>() where T : IntegrationEvent where THandler : IIntegrationEventHandler<T>
    {
        SubscriptionInfo handlerToRemove = FindSubscriptionToRemove<T, THandler>();
        string eventName = GetEventKey<T>();
        RemoveHandler(eventName, handlerToRemove);
    }

    private void RemoveHandler(string eventName, SubscriptionInfo? subscriptionToRemove)
    {
        if (subscriptionToRemove != null)
        {
            _handlers[eventName].Remove(subscriptionToRemove);

            if (_handlers[eventName].Any() == false)
            {
                _handlers.Remove(eventName);
                var eventType = _eventTypes.SingleOrDefault(x => x.Name == eventName);
                if (eventType != null)
                {
                    _eventTypes.Remove(eventType);
                }
                RaiseOnEventRemoved(eventName);
            }
        }
    }

    private void RaiseOnEventRemoved(string eventName)
    {
        var handler = OnEventRemoved;
        handler?.Invoke(this, eventName);
    }

    private SubscriptionInfo FindSubscriptionToRemove<T, THandler>() where T : IntegrationEvent
        where THandler : IIntegrationEventHandler<T>
    {
        string eventName = GetEventKey<T>();
        return FindSubscriptionToRemove(eventName, typeof(THandler));
    }

    private SubscriptionInfo FindSubscriptionToRemove(string eventName, Type handlerType)
    {
        if (HasSubscriptionForEvent(eventName) == false)
        {
            return null!;
        }

        return _handlers[eventName].SingleOrDefault(x => x.HandlerType == handlerType)!;
    }

    public bool HasSubscriptionForEvent<T>() where T : IntegrationEvent
    {
        string eventName = GetEventKey<T>();

        return HasSubscriptionForEvent(eventName);
    }

    public bool HasSubscriptionForEvent(string eventName) => _handlers.ContainsKey(eventName);

    public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(x => x.Name == eventName)!;

    public void Clear()
    {
        _handlers.Clear();
    }

    public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
    {
        string eventName = GetEventKey<T>();

        return GetHandlersForEvent(eventName);
    }

    public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

    public string GetEventKey<T>()
    {
        string eventName = typeof(T).Name;

        return _eventNameGetter(eventName);
    }
}