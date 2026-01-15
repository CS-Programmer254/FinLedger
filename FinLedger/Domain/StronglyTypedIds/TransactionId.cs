namespace FinLedger.Domain.StronglyTypedIds;

/// <summary>
/// Transaction ID - Strongly typed GUID
/// </summary>
public sealed class TransactionId
{
    public Guid Value { get; }

    public TransactionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new InvalidOperationException("Transaction ID cannot be empty");
        Value = value;
    }

    public static TransactionId New() => new(Guid.NewGuid());

    public override bool Equals(object? obj) =>
        obj is TransactionId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
