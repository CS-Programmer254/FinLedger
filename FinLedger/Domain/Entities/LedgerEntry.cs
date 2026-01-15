using FinLedger.Domain.Enums;
using FinLedger.Domain.Shared;
using System.Security.Cryptography;
using System.Text;

namespace FinLedger.Domain.Entities;

/// <summary>
/// Ledger Entry - Child entity of Payment aggregate
/// Represents a single debit or credit operation
/// </summary>
public sealed class LedgerEntry : Entity<Guid>
{
    public Guid PaymentId { get; private set; }
    public LedgerAccount Account { get; private set; }
    public int Debit { get; private set; }
    public int Credit { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string TransactionHash { get; private set; }

    private LedgerEntry() { }

    public LedgerEntry(Guid paymentId, LedgerAccount account, int debit, int credit)
        : base(Guid.NewGuid())
    {
        if (paymentId == Guid.Empty) throw new ArgumentException("Payment ID required");
        if (debit < 0 || credit < 0) throw new ArgumentException("Debit/Credit cannot be negative");
        if (debit > 0 && credit > 0) throw new InvalidOperationException("Cannot have both debit and credit");
        if (debit == 0 && credit == 0) throw new InvalidOperationException("Must have either debit or credit");

        PaymentId = paymentId;
        Account = account;
        Debit = debit;
        Credit = credit;
        CreatedAt = DateTime.UtcNow;
        TransactionHash = ComputeHash();
    }

    private string ComputeHash()
    {
        var data = $"{PaymentId}{Account}{Debit}{Credit}{CreatedAt}";
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }
}