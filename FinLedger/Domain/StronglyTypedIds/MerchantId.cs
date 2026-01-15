namespace FinLedger.Domain.StronglyTypedIds;
/// <summary>
/// Merchant ID - Strongly typed GUID
/// </summary>
public sealed class MerchantId
{
    public Guid Value { get; }

    public MerchantId(Guid value)
    {
        if (value == Guid.Empty)
            throw new InvalidOperationException("Merchant ID cannot be empty");
        Value = value;
    }

    public static MerchantId New() => new(Guid.NewGuid());

    public override bool Equals(object? obj) =>
        obj is MerchantId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
