using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SafeRide.Schools.Application.Abstractions;
using SafeRide.Schools.Application.Common;
using SafeRide.Schools.Application.Events;
using SafeRide.Schools.Domain.Entities;
using SafeRide.Schools.Domain.Repositories;

namespace SafeRide.Schools.Infrastructure.Messaging;

public sealed class IdentityEventsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    ILogger<IdentityEventsConsumer> logger
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
            MessagingConstants.IdentityEventsExchange, //"identity.events",
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken
        );

        await RabbitDLQTopology.DeclareQueueWithDlqAsync(
            _channel!, // your captured local (or _channel!)
            MessagingConstants.IdentityEventsQueue,
            stoppingToken
        );

        await _channel.QueueBindAsync(
            MessagingConstants.IdentityEventsQueue, //"school.identity-events",
            MessagingConstants.IdentityEventsExchange, //"identity.events",
            MessagingConstants.SchoolAdminRegisteredKey, //"school-admin-registered",
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await HandleAsync(json, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process SchoolAdminRegistered");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            MessagingConstants.IdentityEventsQueue, //"school.identity-events",
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleAsync(string json, CancellationToken ct)
    {
        var e = JsonSerializer.Deserialize<SchoolAdminRegistered>(json)!;
        using var scope = scopeFactory.CreateScope();
        var schools = scope.ServiceProvider.GetRequiredService<IGenericRepository<School>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var school = School.CreateDraft(
            e.UserId,
            e.Email,
            e.FirstName,
            e.LastName,
            e.Phone,
            e.SchoolName,
            e.SchoolAddress,
            e.City,
            e.District,
            e.State,
            e.Pincode
        );
        await schools.AddAsync(school, ct);
        await uow.SaveChangesAsync(ct);

        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(
            MessagingConstants.SchoolEventsExchange, //"school.events",
            MessagingConstants.SchoolCreatedKey, //"school-created",
            new SchoolCreated(school.Id, school.AdminUserId, DateTime.UtcNow),
            ct
        );
    }
}
