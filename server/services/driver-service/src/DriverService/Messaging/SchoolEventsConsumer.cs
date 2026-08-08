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
            "school.events",
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken
        );
        await channel.QueueDeclareAsync(
            "driver.school-events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );
        await channel.QueueBindAsync(
            "driver.school-events",
            "school.events",
            "school-approved",
            cancellationToken: stoppingToken
        );
        await channel.QueueBindAsync(
            "driver.school-events",
            "school.events",
            "school-suspended",
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
            "driver.school-events",
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleAsync(string routingKey, string json, CancellationToken ct)
    {
        var schoolId = routingKey switch
        {
            "school-approved" => JsonSerializer.Deserialize<SchoolApproved>(json, Json)!.SchoolId,
            "school-suspended" => JsonSerializer.Deserialize<SchoolSuspended>(json, Json)!.SchoolId,
            _ => Guid.Empty,
        };
        if (schoolId == Guid.Empty)
            return;

        var status = routingKey == "school-approved" ? "Approved" : "Suspended";

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DriverDbContext>();

        var row = await db.SchoolStatuses.FindAsync([schoolId], ct);
        if (row is null)
            db.SchoolStatuses.Add(
                new SchoolStatusProjection
                {
                    SchoolId = schoolId,
                    Status = status,
                    UpdatedAt = DateTime.UtcNow,
                }
            );
        else
        {
            row.Status = status;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("School {SchoolId} projected as {Status}", schoolId, status);
    }
}
