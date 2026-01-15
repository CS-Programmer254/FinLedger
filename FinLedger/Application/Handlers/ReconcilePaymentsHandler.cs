using FinLedger.Application.Commands;
using FinLedger.Application.Interfaces;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Enums;
using MediatR;

namespace FinLedger.Application.Handlers;

/// <summary>
/// Reconciliation Handler
/// Demonstrates querying aggregates for reporting
/// </summary>
public sealed class ReconcilePaymentsHandler : IRequestHandler<ReconcilePaymentsCommand, ReconciliationReportResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IReconciliationRepository _reconciliationRepository;
    private readonly ILogger<ReconcilePaymentsHandler> _logger;

    public ReconcilePaymentsHandler(
        IPaymentRepository paymentRepository,
        IReconciliationRepository reconciliationRepository,
        ILogger<ReconcilePaymentsHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _reconciliationRepository = reconciliationRepository;
        _logger = logger;
    }

    public async Task<ReconciliationReportResponse> Handle(
        ReconcilePaymentsCommand cmd,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting reconciliation process");

        //Load Aggregates by Status 
        var allPayments = await _paymentRepository.GetByStatusAsync(PaymentStatus.Pending);
        var completedPayments = await _paymentRepository.GetByStatusAsync(PaymentStatus.Completed);
        var failedPayments = await _paymentRepository.GetByStatusAsync(PaymentStatus.Failed);

        var pendingCount = allPayments.Count();
        var completedCount = completedPayments.Count();
        var failedCount = failedPayments.Count();

        // Calculate Balances from Aggregates 
        // Get balance from each aggregate's ledger entries
        var customerBalance = 0m;
        var clearingBalance = 0m;
        var merchantBalance = 0m;

        foreach (var payment in allPayments.Concat(completedPayments).Concat(failedPayments))
        {
            // Each payment aggregate has its ledger entries
            customerBalance += payment.GetAccountBalance(LedgerAccount.Customer);
            clearingBalance += payment.GetAccountBalance(LedgerAccount.Clearing);
            merchantBalance += payment.GetAccountBalance(LedgerAccount.Merchant);
        }

        // Verify Invariant 
        // Double-entry bookkeeping invariant
        var isBalanced = Math.Abs(customerBalance - clearingBalance - merchantBalance) < 0.01m;

        if (!isBalanced)
        {
            _logger.LogError(
                "LEDGER IMBALANCE - Customer: {C}, Clearing: {CL}, Merchant: {M}",
                customerBalance, clearingBalance, merchantBalance);
        }
        else
        {
            _logger.LogInformation("✅ Ledger balanced successfully");
        }

        // Create & Persist Snapshot
        var snapshot = new ReconciliationSnapshot(
            pendingCount + completedCount + failedCount,
            pendingCount,
            completedCount,
            failedCount,
            customerBalance,
            clearingBalance,
            merchantBalance);

        await _reconciliationRepository.AddSnapshotAsync(snapshot);

        return new ReconciliationReportResponse(
            snapshot.TotalPayments,
            snapshot.PendingPayments,
            snapshot.CompletedPayments,
            snapshot.FailedPayments,
            snapshot.CustomerBalance,
            snapshot.ClearingBalance,
            snapshot.MerchantBalance,
            snapshot.IsBalanced,
            snapshot.CreatedAt);
    }
}



