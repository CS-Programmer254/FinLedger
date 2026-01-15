using FinLedger.Domain.Entities;
using FinLedger.Domain.Enums;
using FinLedger.Domain.Events;
using FinLedger.Domain.Shared;
using FinLedger.Domain.StronglyTypedIds;
using FinLedger.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace FinLedger.Domain.Aggregates;

public class Car{

public Guid Id { get; set; }
public string CarName { get; set; }

public required string CarColor { get; set; }
public string CarBrand { get; set; } 

public string  CarMileage { get; set; } 
public string ?CarWheel { get; set; } 

}
/// <summary>
/// Payment Aggregate Root - Main aggregate managing payment lifecycle
/// All business logic for payments lives here
/// </summary>
public sealed class Payment : AggregateRoot<Guid>
{
    private readonly List<LedgerEntry> _ledgerEntries = new();

    public MerchantId MerchantId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentReference Reference { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? WebhookUrl { get; private set; }
    public int RetryCount { get; private set; }
    public string? FailureReason { get; private set; }

    // Only aggregate root can access child entities through read-only collection
    public IReadOnlyCollection<LedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    private Payment() { }

    /// <summary>
    /// Create new payment aggregate
    /// This is the only way to create a Payment instance
    /// </summary>
    public Payment(
        MerchantId merchantId,
        Money amount,
        PaymentReference reference,
        string? webhookUrl = null)
        : base(Guid.NewGuid())
    {
        if (amount == null) throw new ArgumentNullException(nameof(amount));
        if (!amount.IsPositive) throw new InvalidOperationException("Amount must be positive");

        MerchantId = merchantId;
        Amount = amount;
        Reference = reference;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        WebhookUrl = webhookUrl;
        RetryCount = 0;

        // Raise domain events
        RaiseDomainEvent(new PaymentCreatedEvent(
            Id, Id, MerchantId.Value, amount.Amount, amount.Currency,
            reference.Value, CreatedAt
        ));

        RaiseDomainEvent(new FundsReservedEvent(Id, Id, amount.Amount, CreatedAt));
    }

    /// <summary>
    /// Reserve funds in clearing account (called during creation)
    /// Only Payment aggregate can manage its ledger
    /// </summary>
    public void ReserveFunds()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Cannot reserve funds for non-pending payment");

        // Create ledger entries within aggregate
        _ledgerEntries.Add(new LedgerEntry(Id, LedgerAccount.Customer, Amount.Amount, 0));
        _ledgerEntries.Add(new LedgerEntry(Id, LedgerAccount.Clearing, 0, Amount.Amount));
    }

    /// <summary>
    /// Mark payment as completed and settle funds
    /// Enforces state machine: PENDING → COMPLETED
    /// </summary>
    public void MarkCompleted()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot complete payment in {Status} state");

        Status = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        // Settle funds to merchant
        _ledgerEntries.Add(new LedgerEntry(Id, LedgerAccount.Clearing, Amount.Amount, 0));
        _ledgerEntries.Add(new LedgerEntry(Id, LedgerAccount.Merchant, 0, Amount.Amount));

        RaiseDomainEvent(new PaymentCompletedEvent(Id, Id, CompletedAt.Value));
        RaiseDomainEvent(new FundsSettledEvent(Id, Id, Amount.Amount, CompletedAt.Value));
    }

    /// <summary>
    /// Mark payment as failed
    /// Enforces business rule: Cannot fail completed payments
    /// </summary>
    public void MarkFailed(string reason)
    {
        if (Status == PaymentStatus.Completed || Status == PaymentStatus.Failed)
            throw new InvalidOperationException("Cannot fail a settled or already failed payment");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason required", nameof(reason));

        Status = PaymentStatus.Failed;
        FailureReason = reason;

        RaiseDomainEvent(new PaymentFailedEvent(Id, Id, reason, DateTime.UtcNow));
    }

    /// <summary>
    /// Get ledger balance for account
    /// Helps with reconciliation
    /// </summary>
    public int GetAccountBalance(LedgerAccount account)
    {
        var entries = _ledgerEntries.Where(e => e.Account == account).ToList();
        return entries.Sum(e => e.Credit - e.Debit);
    }

    /// <summary>
    /// Verify ledger invariant: Must be balanced
    /// </summary>
    public bool IsLedgerBalanced()
    {
        var customerBalance = GetAccountBalance(LedgerAccount.Customer);
        var clearingBalance = GetAccountBalance(LedgerAccount.Clearing);
        var merchantBalance = GetAccountBalance(LedgerAccount.Merchant);

        // Invariant: CUSTOMER - CLEARING - MERCHANT = 0
        return (customerBalance - clearingBalance - merchantBalance) == 0;
    }

    public void IncrementRetry()
    {
        if (RetryCount >= 5) throw new InvalidOperationException("Max retries exceeded");
        RetryCount++;
    }
}
