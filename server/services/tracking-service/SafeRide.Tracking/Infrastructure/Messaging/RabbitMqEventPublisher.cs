using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SafeRide.Tracking.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(
    IOptions<RabbitMqSettings> options,
    ILogger<RabbitMqEventPublisher> logger
) : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqSettings _settings = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(
        string routingKey,
        object payload,
        CancellationToken ct = default
    )
    {
        var channel = await GetChannelAsync(ct);

        await channel.ExchangeDeclareAsync(
            _settings.Exchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        var props = new BasicProperties { Persistent = true, ContentType = "application/json" };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, payload.GetType()));

        await channel.BasicPublishAsync(
            _settings.Exchange,
            routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct
        );

        logger.LogInformation(
            "Published {RoutingKey} to {Exchange}",
            routingKey,
            _settings.Exchange
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

            await CloseAsync();

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

    private async Task CloseAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await CloseAsync();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }
}
