using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Travelogic.Suppliers.Infrastructure.Persistence;

namespace Travelogic.Suppliers.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; init; } = true;

    [Range(typeof(TimeSpan), "00:00:00.100", "01:00:00")]
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);

    [Range(1, 1000)]
    public int BatchSize { get; init; } = 50;

    /// <summary>After this many failures a message is left for an operator to inspect.</summary>
    [Range(1, 100)]
    public int MaxAttempts { get; init; } = 10;
}

/// <summary>
/// Publishes outbox messages in the background. Rows are claimed with UPDLOCK/READPAST, so several
/// instances of the service can run side by side without publishing the same message twice.
/// Delivery is at least once: consumers should de-duplicate on the message id.
/// </summary>
internal sealed partial class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventPublisher publisher,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var published = 0;
            try
            {
                published = await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // A failed batch must not kill the processor; it retries on the next tick.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogBatchFailed(ex);
            }

            // A full batch probably means there is more waiting, so go again straight away.
            if (published < _options.BatchSize)
            {
                try
                {
                    await Task.Delay(_options.PollingInterval, timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    internal async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SuppliersDbContext>();

        // Explicit transactions must run inside the execution strategy when retries are enabled.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var messages = await db.OutboxMessages
                .FromSql($"""
                    SELECT TOP ({_options.BatchSize}) *
                    FROM [supplier].[OutboxMessages] WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE [ProcessedAtUtc] IS NULL AND [Attempts] < {_options.MaxAttempts}
                    ORDER BY [OccurredAtUtc], [Sequence]
                    """)
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                try
                {
                    await publisher.PublishAsync(message, ct);
                    message.ProcessedAtUtc = timeProvider.GetUtcNow();
                    message.LastError = null;
                }
#pragma warning disable CA1031 // One bad message must not block the rest of the batch.
                catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
                {
                    message.Attempts++;
                    message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                    LogPublishFailed(ex, message.Id, message.Type, message.Attempts);
                }
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return messages.Count;
        }, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox batch failed")]
    private partial void LogBatchFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish outbox message {MessageId} ({MessageType}), attempt {Attempt}")]
    private partial void LogPublishFailed(Exception exception, Guid messageId, string messageType, int attempt);
}
