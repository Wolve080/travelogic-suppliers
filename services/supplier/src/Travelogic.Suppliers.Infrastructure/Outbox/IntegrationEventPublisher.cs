using Microsoft.Extensions.Logging;

namespace Travelogic.Suppliers.Infrastructure.Outbox;

/// <summary>
/// Sends an event to the message broker. This is the seam where RabbitMQ, Azure Service Bus or Kafka
/// plugs in; the rest of the service does not change.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}

/// <summary>Default publisher for running standalone: writes the event to the log instead of a broker.</summary>
internal sealed partial class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        LogPublished(message.Type, message.AggregateId, message.Id, message.Payload);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {EventType} for supplier {SupplierId} (message {MessageId}): {Payload}")]
    private partial void LogPublished(string eventType, Guid supplierId, Guid messageId, string payload);
}
