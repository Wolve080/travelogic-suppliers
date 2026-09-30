using Microsoft.Extensions.Logging;

namespace Travelogic.Suppliers.Infrastructure.Outbox;

// TODO: swap for a real broker (Service Bus / RabbitMQ)
public interface IIntegrationEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}

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
