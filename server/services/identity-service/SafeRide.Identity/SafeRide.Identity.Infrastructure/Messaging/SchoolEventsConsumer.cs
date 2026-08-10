using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Identity.Application.Common;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Repositories;

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
        var channel = _channel;

        await channel.ExchangeDeclareAsync(
            MessagingConstants.SchoolEventsExchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken
        );

        await RabbitDLQTopology.DeclareQueueWithDlqAsync(
            channel,
            MessagingConstants.SchoolEventsQueue,
            stoppingToken
        );

        await channel.QueueBindAsync(
            MessagingConstants.SchoolEventsQueue,
            MessagingConstants.SchoolEventsExchange,
            MessagingConstants.SchoolCreatedKey,
            cancellationToken: stoppingToken
        );
        await channel.QueueBindAsync(
            MessagingConstants.SchoolEventsQueue,
            MessagingConstants.SchoolEventsExchange,
            MessagingConstants.SchoolApprovedKey,
            cancellationToken: stoppingToken
        );
        await channel.QueueBindAsync(
            MessagingConstants.SchoolEventsQueue,
            MessagingConstants.SchoolEventsExchange,
            MessagingConstants.SchoolSuspendedKey,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await HandleAsync(ea.RoutingKey, json, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {Key}", ea.RoutingKey);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            MessagingConstants.SchoolEventsQueue,
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

        if (routingKey == MessagingConstants.SchoolCreatedKey)
        {
            var e = JsonSerializer.Deserialize<SchoolCreated>(json)!;
            var user = await users.GetByIdAsync(e.AdminUserId, ct);
            if (user is null)
                return;
            user.LinkSchool(e.SchoolId);
            await uow.SaveChangesAsync(ct);
            logger.LogInformation(
                "Linked school {SchoolId} to user {UserId} at creation",
                e.SchoolId,
                user.Id
            );
        }
        else if (routingKey == MessagingConstants.SchoolApprovedKey)
        {
            var e = JsonSerializer.Deserialize<SchoolApproved>(json)!;
            var user = await users.GetByIdAsync(e.AdminUserId, ct);
            if (user is null)
                return;
            user.LinkSchool(e.SchoolId);
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Linked school {SchoolId} to user {UserId}", e.SchoolId, user.Id);
        }
        else if (routingKey == MessagingConstants.SchoolSuspendedKey)
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
