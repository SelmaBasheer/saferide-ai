using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Repositories; // IUserRepository, IUnitOfWork (moved here)

namespace SafeRide.Identity.Infrastructure.Messaging;

public sealed class SchoolEventsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    ILogger<SchoolEventsConsumer> logger
) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var s = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = s.Host,
            UserName = s.Username,
            Password = s.Password,
        };
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            "school.events",
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken
        );
        await _channel.QueueDeclareAsync(
            "identity.school-events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );
        await _channel.QueueBindAsync(
            "identity.school-events",
            "school.events",
            "school-approved",
            cancellationToken: stoppingToken
        );
        await _channel.QueueBindAsync(
            "identity.school-events",
            "school.events",
            "school-suspended",
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await HandleAsync(ea.RoutingKey, json, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {Key}", ea.RoutingKey);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            "identity.school-events",
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleAsync(string routingKey, string json, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (routingKey == "school-approved")
        {
            var e = JsonSerializer.Deserialize<SchoolApproved>(json)!;
            var user = await users.GetByIdAsync(e.AdminUserId, ct);
            if (user is null)
                return;
            user.Activate(e.SchoolId); // Status → Active, links schoolId
            await uow.SaveChangesAsync(ct);
        }
        else if (routingKey == "school-suspended")
        {
            var e = JsonSerializer.Deserialize<SchoolSuspended>(json)!;
            var user = await users.GetByIdAsync(e.AdminUserId, ct);
            if (user is null)
                return;
            user.Suspend();
            await uow.SaveChangesAsync(ct);
        }
    }
}
