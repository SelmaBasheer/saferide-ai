using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SafeRide.Identity.Application.Abstractions;

namespace SafeRide.Identity.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(IOptions<RabbitMqSettings> options)
    : IEventPublisher,
        IAsyncDisposable
{
    private readonly RabbitMqSettings _settings = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync<TEvent>(
        string exchange,
        string routingKey,
        TEvent message,
        CancellationToken ct = default
    )
        where TEvent : class
    {
        var channel = await GetChannelAsync(ct);

        await channel.ExchangeDeclareAsync(
            exchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var props = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await channel.BasicPublishAsync(
            exchange,
            routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct
        );
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is not null)
            return _channel;

        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            UserName = _settings.Username,
            Password = _settings.Password,
        };

        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
        if (_connection is not null)
            await _connection.CloseAsync();
    }
}
