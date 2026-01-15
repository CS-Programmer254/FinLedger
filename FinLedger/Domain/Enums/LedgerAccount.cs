namespace FinLedger.Domain.Enums;

public enum LedgerAccount
{
    Customer = 0,      // Payer's funds
    Clearing = 1,      // In-transit funds
    Merchant = 2       // Merchant's received funds
}
