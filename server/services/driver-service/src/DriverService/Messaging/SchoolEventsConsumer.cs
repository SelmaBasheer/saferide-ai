using System.Text;
using System.Text.Json;
using DriverService.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DriverService.Messaging;

public sealed class SchoolEventsConsumer(
    IServiceScopeFactory scopeFactory,
    Microsoft.Extensions.Options.IOptions<RabbitMqSettings> options,
    ILogger<SchoolEventsConsumer> logger
) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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
        var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _channel = channel;

        await channel.ExchangeDeclareAsync(
            MessagingConstants.SchoolEventsExchange, //"school.events",
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken
        );

        await DeclareQueueWithDlqAsync(
            channel,
            MessagingConstants.SchoolEventsQueue, //"driver.school-events"
            stoppingToken
        );

        await channel.QueueBindAsync(
            MessagingConstants.SchoolEventsQueue, //"driver.school-events"
            MessagingConstants.SchoolEventsExchange, //"school.events",
            MessagingConstants.SchoolApprovedKey, // "school-approved",
            cancellationToken: stoppingToken
        );
        await channel.QueueBindAsync(
            MessagingConstants.SchoolEventsQueue, //"driver.school-events"
            MessagingConstants.SchoolEventsExchange, //"school.events",
            MessagingConstants.SchoolSuspendedKey, //"school-suspended",
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
            MessagingConstants.SchoolEventsQueue, //"driver.school-events"
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken
        );
    }

    private static async Task DeclareQueueWithDlqAsync(
        IChannel channel,
        string queue,
        CancellationToken ct
    )
    {
        const string Dlx = "saferide.dlx";

        await channel.ExchangeDeclareAsync(
            Dlx,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        await channel.QueueDeclareAsync(
            $"{queue}.dlq",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );
        await channel.QueueBindAsync($"{queue}.dlq", Dlx, queue, cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = Dlx,
                ["x-dead-letter-routing-key"] = queue,
            },
            cancellationToken: ct
        );
    }

    private async Task HandleAsync(string routingKey, string json, CancellationToken ct)
    {
        Guid schoolId;
        DateTime occurredAtUtc;
        string status;

        if (routingKey == MessagingConstants.SchoolApprovedKey)
        {
            var e = JsonSerializer.Deserialize<SchoolApproved>(json, Json)!;
            schoolId = e.SchoolId;
            occurredAtUtc = e.OccurredAtUtc;
            status = MessagingConstants.StatusApproved;
        }
        else if (routingKey == MessagingConstants.SchoolSuspendedKey)
        {
            var e = JsonSerializer.Deserialize<SchoolSuspended>(json, Json)!;
            schoolId = e.SchoolId;
            occurredAtUtc = e.OccurredAtUtc;
            status = MessagingConstants.StatusSuspended;
        }
        else
        {
            return;
        }

        if (schoolId == Guid.Empty)
            return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DriverDbContext>();

        var row = await db.SchoolStatuses.FindAsync([schoolId], ct);
        if (row is null)
        {
            db.SchoolStatuses.Add(
                new SchoolStatusProjection
                {
                    SchoolId = schoolId,
                    Status = status,
                    EventAtUtc = occurredAtUtc,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        }
        else if (occurredAtUtc > row.EventAtUtc)
        {
            row.Status = status;
            row.EventAtUtc = occurredAtUtc;
            row.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            logger.LogInformation(
                "Ignored stale {Key} for school {SchoolId}",
                routingKey,
                schoolId
            );
            return; // nothing to save
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("School {SchoolId} projected as {Status}", schoolId, status);
    }
}
