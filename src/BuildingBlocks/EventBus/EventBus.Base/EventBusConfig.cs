﻿namespace EventBus.Base;

public class EventBusConfig
{
    public static int ConnectionRetryCount { get; set; } = 5;
    public static string? DefaultTopicName { get; set; } = "SellingEventBus";
    public string EventBusConnectionString { get; set; } = string.Empty;
    public string SubscriberClientAppName { get; set; } = string.Empty;
    public string EventNamePrefix { get; set; } = string.Empty;
    public string EventNameSuffix { get; set; } = "IntegrationEvent";
    public EventBusType EventBusType { get; set; } = EventBusType.RabbitMq;
    public object? Connection { get; set; }

    public bool DeleteEventPrefix => !string.IsNullOrEmpty(EventNamePrefix);
    public bool DeleteEventSuffix => !string.IsNullOrEmpty(EventNameSuffix);
}

public enum EventBusType
{
    RabbitMq = 0,
    AzureServiceBus = 1,
}