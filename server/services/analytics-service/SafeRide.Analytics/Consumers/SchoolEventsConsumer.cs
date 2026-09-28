using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Analytics.Abstractions;
using SafeRide.Analytics.Messaging;
using SafeRide.Analytics.Messaging.Events;
using SafeRide.Analytics.Models;
using SafeRide.Analytics.Services;

namespace SafeRide.Analytics.Consumers;

public sealed class SchoolEventsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    LocalDates localDates,
    ILogger<SchoolEventsConsumer> logger
) : BackgroundService
{
    // The School service is .NET and publishes PascalCase, unlike Bus. The
    // case-insensitive flag covers both, so one options object serves every
    // publisher regardless of language.
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new UtcDateTimeConverter() },
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

        await _channel.ExchangeDeclareAsync(
            s.SchoolExchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken
        );

        await RabbitDLQTopology.DeclareQueueWithDlqAsync(_channel, s.SchoolQueue, stoppingToken);

        string[] keys =
        [
            MessagingConstants.SchoolApprovedKey,
            MessagingConstants.SchoolSuspendedKey,
            MessagingConstants.SchoolSubscriptionChangedKey,
            MessagingConstants.PaymentCapturedKey,
        ];

        foreach (var key in keys)
        {
            await _channel.QueueBindAsync(
                s.SchoolQueue,
                s.SchoolExchange,
                key,
                cancellationToken: stoppingToken
            );
        }

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            await MessageRetry.ExecuteAsync(
                _channel!,
                ea,
                logger,
                () => HandleAsync(ea.RoutingKey, json, stoppingToken),
                stoppingToken
            );
        };

        await _channel.BasicConsumeAsync(
            s.SchoolQueue,
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleAsync(string routingKey, string json, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        switch (routingKey)
        {
            case MessagingConstants.SchoolApprovedKey:
            {
                var e = Parse<SchoolApproved>(json, routingKey, x => x.OccurredAtUtc);
                await scope
                    .ServiceProvider.GetRequiredService<ISchoolDimensionRepository>()
                    .UpsertStatusAsync(
                        new SchoolStatusRow(
                            e.SchoolId,
                            e.SchoolName,
                            e.City,
                            "Approved",
                            e.OccurredAtUtc
                        ),
                        ct
                    );
                break;
            }

            case MessagingConstants.SchoolSuspendedKey:
            {
                var e = Parse<SchoolSuspended>(json, routingKey, x => x.OccurredAtUtc);
                await scope
                    .ServiceProvider.GetRequiredService<ISchoolDimensionRepository>()
                    .UpsertStatusAsync(
                        new SchoolStatusRow(
                            e.SchoolId,
                            Name: null,
                            City: null,
                            "Suspended",
                            e.OccurredAtUtc
                        ),
                        ct
                    );
                break;
            }

            case MessagingConstants.SchoolSubscriptionChangedKey:
            {
                var e = Parse<SchoolSubscriptionChanged>(json, routingKey, x => x.OccurredAtUtc);
                await scope
                    .ServiceProvider.GetRequiredService<ISchoolDimensionRepository>()
                    .UpdateEntitlementAsync(
                        new SchoolEntitlementRow(
                            e.SchoolId,
                            e.PlanName,
                            e.Status,
                            e.EndsOn,
                            e.BusLimit,
                            e.OccurredAtUtc
                        ),
                        ct
                    );
                break;
            }

            case MessagingConstants.PaymentCapturedKey:
            {
                var e = Parse<PaymentCaptured>(json, routingKey, x => x.OccurredAtUtc);
                await scope
                    .ServiceProvider.GetRequiredService<IPaymentFactRepository>()
                    .InsertAsync(
                        new PaymentFactRow(
                            e.PaymentId,
                            e.SchoolId,
                            e.SubscriptionId,
                            e.PlanName,
                            e.AmountInPaise,
                            e.Status,
                            e.CapturedAtUtc,
                            // The local date is what a report range filters on.
                            localDates.ToLocalDate(e.CapturedAtUtc)
                        ),
                        ct
                    );
                break;
            }

            default:
                logger.LogWarning("Ignoring unexpected routing key {RoutingKey}", routingKey);
                return;
        }

        logger.LogInformation("Projected {RoutingKey}", routingKey);
    }

    /// <summary>
    /// A default timestamp means a property didn't bind — almost always a field
    /// name that has drifted from the publisher. JsonException rather than a
    /// generic one on purpose: this is broken for good, so the retry policy
    /// dead-letters it immediately instead of trying four times.
    /// </summary>
    private static T Parse<T>(string json, string routingKey, Func<T, DateTime> timestamp)
    {
        var e =
            JsonSerializer.Deserialize<T>(json, Json)
            ?? throw new JsonException($"Empty payload for {routingKey}.");

        if (timestamp(e) == default)
        {
            throw new JsonException(
                $"Event timestamp did not deserialise for {routingKey}. Payload: {json}"
            );
        }

        return e;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
