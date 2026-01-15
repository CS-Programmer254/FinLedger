using FinLedger.Domain.Aggregates;

namespace FinLedger.Application.Interfaces;

/// <summary>
/// Webhook Repository - Only loads/saves WebhookAggregate roots
/// </summary>
public interface IWebhookRepository
{
    Task AddAsync(WebhookAggregate aggregate);
    Task<WebhookAggregate?> GetByPaymentIdAsync(Guid paymentId);
    Task UpdateAsync(WebhookAggregate aggregate);
    Task<IEnumerable<WebhookAggregate>> GetWithPendingDeliveriesAsync();
}