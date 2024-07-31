using System.Net.Sockets;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventBus.RabbitMQ;

public class RabbitMqPersistentConnection : IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection connection;
    private readonly int _retryCount;
    private readonly object _lockObject = new();
    private bool _disposed;

    public RabbitMqPersistentConnection(IConnectionFactory connectionFactory, int retryCount = 5)
    {
        _connectionFactory = connectionFactory;
        _retryCount = retryCount;
    }

    public bool IsConnected => connection != null && connection.IsOpen;

    public IModel CreateModel()
    {
        return connection.CreateModel();
    }

    public void Dispose()
    {
        _disposed = true;
        connection.Dispose();
    }

    public bool TryConnect()
    {
        lock (_lockObject)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) =>
                    {

                    });

            policy.Execute(() =>
            {
                connection = _connectionFactory.CreateConnection();
            });

            if (IsConnected)
            {
                connection.ConnectionShutdown += Connection_ConnectionShutDown!;
                connection.CallbackException += ConnectionOnCallbackException;
                connection.ConnectionBlocked += Connection_ConnectionBlocked;

                // loglama yapılabilir

                return true;
            }

            return false;
        }
    }

    private void Connection_ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed==false)
            return;
        TryConnect();
    }

    private void ConnectionOnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed == false)
            return;
        TryConnect();
    }

    private void Connection_ConnectionShutDown(object sender, ShutdownEventArgs e)
    {
        if (_disposed == false)
            return;
        // connection shutDown oldu diye log atabiliriz

        TryConnect();
    }
}