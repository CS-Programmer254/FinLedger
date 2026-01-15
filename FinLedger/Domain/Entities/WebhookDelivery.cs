using FinLedger.Domain.Shared;

namespace FinLedger.Domain.Entities;

/// <summary>
/// Webhook - Child entity of WebhookAggregate
/// Tracks webhook delivery attempts
/// </summary>
public sealed class WebhookDelivery : Entity<Guid>
{
    public Guid PaymentId { get; private set; }
    public string Url { get; private set; }
    public string EncryptedPayload { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public bool IsSuccessful { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    private WebhookDelivery() { }

    public WebhookDelivery(Guid paymentId, string url, string encryptedPayload)
        : base(Guid.NewGuid())
    {
        if (paymentId == Guid.Empty) throw new ArgumentException("Payment ID required");
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL required");
        if (string.IsNullOrWhiteSpace(encryptedPayload)) throw new ArgumentException("Payload required");

        PaymentId = paymentId;
        Url = url;
        EncryptedPayload = encryptedPayload;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
        IsSuccessful = false;
    }

    public void RecordAttempt(bool successful, int maxRetries = 5)
    {
        RetryCount++;
        LastAttemptAt = DateTime.UtcNow;
        IsSuccessful = successful;

        if (!successful && RetryCount < maxRetries)
        {
            var backoffSeconds = Math.Pow(2, RetryCount);
            NextRetryAt = DateTime.UtcNow.AddSeconds(backoffSeconds);
        }
    }

    public bool ShouldRetry() => !IsSuccessful && NextRetryAt <= DateTime.UtcNow;
}
