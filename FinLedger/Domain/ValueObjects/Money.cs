using System.Security.Cryptography;
using System.Text;

namespace FinLedger.Domain.ValueObjects;

/// <summary>
/// Money value object - Amount + Currency together
/// Enforces validation at creation time
/// </summary>
public sealed class Money
{
    public int Amount { get; }
    public string Currency { get; }

    public Money(int amount, string currency)
    {
        if (amount < 0)
            throw new InvalidOperationException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new InvalidOperationException("Currency must be 3 characters (ISO 4217)");

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public bool IsPositive => Amount > 0;
    public bool IsZero => Amount == 0;

    public override bool Equals(object? obj) =>
        obj is Money other && Amount == other.Amount && Currency == other.Currency;

    public override int GetHashCode() =>
        HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount} {Currency}";
}


/// <summary>
/// Payment Reference - Idempotency key
/// </summary>
public sealed class PaymentReference
{
    public string Value { get; }

    public PaymentReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 50)
            throw new InvalidOperationException("Payment reference must be 1-50 characters");
        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is PaymentReference other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}


/// <summary>
/// Encrypted Payload - AES-256-GCM encryption
/// </summary>
public sealed class EncryptedPayload
{
    public string CipherText { get; }
    public string Nonce { get; }
    public string Tag { get; }

    public EncryptedPayload(string cipherText, string nonce, string tag)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            throw new ArgumentException("Cipher text required", nameof(cipherText));
        if (string.IsNullOrWhiteSpace(nonce))
            throw new ArgumentException("Nonce required", nameof(nonce));
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag required", nameof(tag));

        CipherText = cipherText;
        Nonce = nonce;
        Tag = tag;
    }

    public static EncryptedPayload Encrypt(string plaintext, string key)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            throw new ArgumentException("Plaintext required", nameof(plaintext));
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new ArgumentException("Key must be at least 32 characters", nameof(key));

        var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32)[..32]);
        using var aes = new AesGcm(keyBytes);

        var nonceBytes = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonceBytes);

        var plainTextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainTextBytes.Length];
        var tagBytes = new byte[16];

        aes.Encrypt(nonceBytes, plainTextBytes, null, cipherBytes, tagBytes);

        return new EncryptedPayload(
            Convert.ToBase64String(cipherBytes),
            Convert.ToBase64String(nonceBytes),
            Convert.ToBase64String(tagBytes)
        );
    }

    public string Decrypt(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new ArgumentException("Key must be at least 32 characters", nameof(key));

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32)[..32]);
            using var aes = new AesGcm(keyBytes);

            var cipherBytes = Convert.FromBase64String(CipherText);
            var nonceBytes = Convert.FromBase64String(Nonce);
            var tagBytes = Convert.FromBase64String(Tag);

            var plaintextBytes = new byte[cipherBytes.Length];
            aes.Decrypt(nonceBytes, cipherBytes, null, plaintextBytes, tagBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Decryption failed - payload may be tampered", ex);
        }
    }

    public override string ToString() => $"EncryptedPayload(size={CipherText.Length})";
}