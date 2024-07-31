using EventBus.Base.Events;  

namespace EventBus.Base.Abstraction;

public interface IEventBusSubscriptionManager
{
    bool IsEmpty { get; } //Herhangi bir eventi dinleyip dinlemediğimizi kontrol ediyoruz

    event EventHandler<string> OnEventRemoved; //Bu event remove edildiği zaman içeride bir event oluşturacağız. Dışarıdan bize bir unsubscribe metodu geldiğinde bu eventi de tetikleyeceğiz

    void AddSubscription<T, THandler>() where T : IntegrationEvent where THandler : IIntegrationEventHandler<T>; //Subscriptionu Ekler
    void RemoveSubscription<T, THandler>() where T : IntegrationEvent where THandler : IIntegrationEventHandler<T>; //Subscriptionu Siler
    bool HasSubscriptionForEvent<T>() where T : IntegrationEvent; //Dışarıdan bir event gönderildiğinde bizim o eventi dinleyip dinlemediğimizin kontrolü yapılır.(Generic olarak)
    bool HasSubscriptionForEvent(string eventName); //Dışarıdan bir event gönderildiğinde bizim o eventi dinleyip dinlemediğimizin kontrolü yapılır.(Name ile)
    Type GetEventTypeByName(string eventName); //Event name gönderildiğinde onun Type ını geri döner.
    void Clear(); //Bütün Subscriptionları silmek için.
    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent; //Bir eventin bütün subscriptionlarını - handlerlarını geriye döner(Generic olarak)
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName); //Bir eventin bütün subscriptionlarını - handlerlarını geriye döner(Name ile)
    string GetEventKey<T>(); //Event in ismini geri dönen metod
}