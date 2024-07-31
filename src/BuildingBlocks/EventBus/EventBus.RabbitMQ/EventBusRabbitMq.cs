using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using EventBus.Base;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventBus.RabbitMQ;

public class EventBusRabbitMq : BaseEventBus
{
    private RabbitMqPersistentConnection _persistentConnection;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IModel consumerChannel;

    public EventBusRabbitMq(IServiceProvider serviceProvider, EventBusConfig eventBusConfig) : base(serviceProvider, eventBusConfig)
    {
        if (eventBusConfig.Connection != null)
        {
            var connJson = JsonConvert.SerializeObject(eventBusConfig, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            _connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson)!;
        }
        else
        {
            _connectionFactory = new ConnectionFactory();
        }

        _persistentConnection =
            new RabbitMqPersistentConnection(_connectionFactory, EventBusConfig.ConnectionRetryCount);

        consumerChannel = CreateConsumerChannel();

        SubscriptionManager.OnEventRemoved += SubscriptionManager_OnEventRemoved;
    }

    private void SubscriptionManager_OnEventRemoved(object? sender, string eventName)
    {
        eventName = ProcessEventName(eventName);

        if (_persistentConnection.IsConnected == false)
        {
            _persistentConnection.TryConnect();
        }

        consumerChannel.QueueUnbind(
            queue: eventName,
            exchange: EventBusConfig.DefaultTopicName,
            routingKey: eventName);

        if (SubscriptionManager.IsEmpty)
        {
            consumerChannel.Close();
        }
    }

    public override void Publish(IntegrationEvent @event)
    {
        if (_persistentConnection.IsConnected == false)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(EventBusConfig.ConnectionRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) =>
                {
                    //Hata alındığı durumlarda loglama yapılabilir.
                });

        var eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);

        consumerChannel.ExchangeDeclare(
            exchange: EventBusConfig.DefaultTopicName,
            type: ExchangeType.Direct);

        var message = JsonConvert.SerializeObject(@event);
        var body = Encoding.UTF8.GetBytes(message);
        policy.Execute(() =>
        {
            var properties = consumerChannel.CreateBasicProperties();
            properties.DeliveryMode = 2; //persistent

            consumerChannel.QueueDeclare(
                queue: GetSubName(eventName),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            consumerChannel.BasicPublish(
                exchange: EventBusConfig.DefaultTopicName,
                routingKey: eventName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });
    }

    public override void Subscribe<T, THandler>()
    {
        var eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);

        if (SubscriptionManager.HasSubscriptionForEvent(eventName) == false)
        {
            if (_persistentConnection.IsConnected == false)
            {
                _persistentConnection.TryConnect();
            }

            consumerChannel.QueueDeclare(
                queue: GetSubName(eventName),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            consumerChannel.QueueBind(
                queue: GetSubName(eventName),
                exchange: EventBusConfig.DefaultTopicName,
                routingKey: eventName);
        }

        SubscriptionManager.AddSubscription<T, THandler>();
        StartBasicConsume(eventName);
    }

    public override void UnSubscribe<T, THandler>()
    {
        SubscriptionManager.RemoveSubscription<T, THandler>();
    }

    private IModel CreateConsumerChannel()
    {
        if (_persistentConnection.IsConnected == false)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();
        channel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: ExchangeType.Direct);

        return channel;
    }

    private void StartBasicConsume(string eventName)
    {
        if (consumerChannel != null)
        {
            var consumer = new EventingBasicConsumer(consumerChannel);

            consumer.Received += Consumer_Received;

            consumerChannel.BasicConsume(
                queue: GetSubName(eventName),
                autoAck: false,
                consumer: consumer);
        }
    }

    private async void Consumer_Received(object? sender, BasicDeliverEventArgs e)
    {
        var eventName = e.RoutingKey;
        eventName = ProcessEventName(eventName);
        var message = Encoding.UTF8.GetString(e.Body.Span);

        try
        {
            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            throw;
        }

        consumerChannel.BasicAck(e.DeliveryTag, multiple: false);
    }
}