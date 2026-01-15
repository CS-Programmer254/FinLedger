using FinLedger.Domain.Entities;
using FinLedger.Domain.Shared;

namespace FinLedger.Domain.Aggregates;


/// <summary>
/// WebhookAggregate - Manages webhook delivery lifecycle
/// Separate from Payment aggregate - follows single responsibility
/// </summary>
public sealed class WebhookAggregate : AggregateRoot<Guid>
{
    private readonly List<WebhookDelivery> _deliveries = new();

    public Guid PaymentId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<WebhookDelivery> Deliveries => _deliveries.AsReadOnly();

    private WebhookAggregate() { }

    public WebhookAggregate(Guid paymentId)
        : base(Guid.NewGuid())
    {
        if (paymentId == Guid.Empty) throw new ArgumentException("Payment ID required");
        PaymentId = paymentId;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddDelivery(string url, string encryptedPayload)
    {
        var delivery = new WebhookDelivery(PaymentId, url, encryptedPayload);
        _deliveries.Add(delivery);
    }

    public WebhookDelivery? GetLatestDelivery() =>
        _deliveries.OrderByDescending(d => d.CreatedAt).FirstOrDefault();

    public bool HasSuccessfulDelivery() =>
        _deliveries.Any(d => d.IsSuccessful);
}

