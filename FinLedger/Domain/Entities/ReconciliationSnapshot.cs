using FinLedger.Domain.Shared;

namespace FinLedger.Domain.Entities;

/// <summary>
/// Reconciliation Snapshot - Point-in-time ledger state
/// Capturing reconciliation state
/// </summary>
public sealed class ReconciliationSnapshot : Entity<Guid>
{
    public int TotalPayments { get; private set; }
    public int PendingPayments { get; private set; }
    public int CompletedPayments { get; private set; }
    public int FailedPayments { get; private set; }
    public decimal CustomerBalance { get; private set; }
    public decimal ClearingBalance { get; private set; }
    public decimal MerchantBalance { get; private set; }
    public bool IsBalanced { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string Notes { get; private set; }

    private ReconciliationSnapshot() { }

    public ReconciliationSnapshot(
        int totalPayments,
        int pending,
        int completed,
        int failed,
        decimal customer,
        decimal clearing,
        decimal merchant)
        : base(Guid.NewGuid())
    {
        TotalPayments = totalPayments;
        PendingPayments = pending;
        CompletedPayments = completed;
        FailedPayments = failed;
        CustomerBalance = customer;
        ClearingBalance = clearing;
        MerchantBalance = merchant;
        CreatedAt = DateTime.UtcNow;

        // Verify ledger invariant
        IsBalanced = Math.Abs(customer - clearing - merchant) < 0.01m;
        Notes = IsBalanced ? "Ledger balanced" : "Ledger imbalance detected";
    }
}
