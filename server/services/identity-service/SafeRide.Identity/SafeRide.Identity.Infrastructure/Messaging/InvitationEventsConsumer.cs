using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Identity.Application.Auth.Invite;
using SafeRide.Identity.Application.Events;
using SafeRide.Identity.Domain.Enums;

namespace SafeRide.Identity.Infrastructure.Messaging;

public sealed class InvitationEventsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    ILogger<InvitationEventsConsumer> logger
) : BackgroundService
{
    // Java events are camelCase, .NET events PascalCase — accept both.
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
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await DeclareAndBindAsync(
            "driver.events",
            "identity.driver-events",
            "driver-created",
            stoppingToken
        );
        await DeclareAndBindAsync(
            "student.events",
            "identity.student-events",
            "student-created",
            stoppingToken
        );

        await ConsumeAsync("identity.driver-events", stoppingToken);
        await ConsumeAsync("identity.student-events", stoppingToken);
    }

    private async Task DeclareAndBindAsync(
        string exchange,
        string queue,
        string key,
        CancellationToken ct
    )
    {
        await _channel!.ExchangeDeclareAsync(
            exchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );
        await _channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );
        await _channel.QueueBindAsync(queue, exchange, key, cancellationToken: ct);
    }

    private async Task ConsumeAsync(string queue, CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await HandleAsync(ea.RoutingKey, json, stoppingToken);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {Key}", ea.RoutingKey);
                await _channel!.BasicNackAsync(
                    ea.DeliveryTag,
                    false,
                    requeue: false,
                    stoppingToken
                );
            }
        };
        await _channel!.BasicConsumeAsync(
            queue,
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleAsync(string routingKey, string json, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var invite = scope.ServiceProvider.GetRequiredService<InviteUserHandler>();

        if (routingKey == "driver-created")
        {
            var e = JsonSerializer.Deserialize<DriverCreated>(json, Json)!;
            await invite.HandleAsync(
                e.Email,
                e.FirstName,
                e.LastName,
                e.Phone,
                UserRole.Driver,
                e.SchoolId,
                ct
            );
            logger.LogInformation("Driver account invited for {Email}", e.Email);
        }
        else if (routingKey == "student-created")
        {
            var e = JsonSerializer.Deserialize<StudentCreated>(json, Json)!;
            await invite.HandleAsync(
                e.ParentEmail,
                e.ParentFirstName,
                e.ParentLastName,
                e.ParentPhone,
                UserRole.Parent,
                e.SchoolId,
                ct
            );
            logger.LogInformation("Parent account invited for {Email}", e.ParentEmail);
        }
    }
}
