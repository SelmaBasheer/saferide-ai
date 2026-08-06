using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DriverService.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(string exchange, string routingKey, string payload, CancellationToken ct);
}

public sealed class RabbitMqEventPublisher(IOptions<RabbitMqSettings> options)
    : IEventPublisher,
        IAsyncDisposable
{
    private readonly RabbitMqSettings _settings = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task PublishAsync(
        string exchange,
        string routingKey,
        string payload,
        CancellationToken ct
    )
    {
        var channel = await GetChannelAsync(ct);

        await channel.ExchangeDeclareAsync(
            exchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        var props = new BasicProperties { Persistent = true, ContentType = "application/json" };
        await channel.BasicPublishAsync(
            exchange,
            routingKey,
            mandatory: false,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(payload),
            cancellationToken: ct
        );
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;
        await _lock.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                UserName = _settings.Username,
                Password = _settings.Password,
            };
            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true
                ),
                cancellationToken: ct
            );
            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
        if (_connection is not null)
            await _connection.CloseAsync();
    }
}
