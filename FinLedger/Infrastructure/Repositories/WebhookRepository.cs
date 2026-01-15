using FinLedger.Application.Interfaces;
using FinLedger.Domain.Aggregates;
using FinLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinLedger.Infrastructure.Repositories;

/// <summary>
/// Webhook Repository Implementation
/// </summary>
public sealed class WebhookRepository : IWebhookRepository
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<WebhookRepository> _logger;

    public WebhookRepository(PaymentsDbContext context, ILogger<WebhookRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(WebhookAggregate aggregate)
    {
        _logger.LogInformation(
            "Saving WebhookAggregate - PaymentId: {PaymentId}", aggregate.PaymentId);

        _context.WebhookAggregates.Add(aggregate);
        await _context.SaveChangesAsync();
    }

    public async Task<WebhookAggregate?> GetByPaymentIdAsync(Guid paymentId)
    {
        return await _context.WebhookAggregates
            .Include(w => w.Deliveries)
            .FirstOrDefaultAsync(w => w.PaymentId == paymentId);
    }

    public async Task UpdateAsync(WebhookAggregate aggregate)
    {
        _context.WebhookAggregates.Update(aggregate);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WebhookAggregate>> GetWithPendingDeliveriesAsync()
    {
        return await _context.WebhookAggregates
            .Include(w => w.Deliveries)
            .Where(w => w.Deliveries.Any(d => d.ShouldRetry()))
            .ToListAsync();
    }
}