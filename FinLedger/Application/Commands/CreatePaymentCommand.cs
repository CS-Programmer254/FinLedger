namespace FinLedger.Application.Commands;

using MediatR;

/// <summary>
/// Create payment command 
/// </summary>
public sealed record CreatePaymentCommand(
    Guid MerchantId,
    int Amount,
    string Currency,
    string Reference,
    string? WebhookUrl = null
) : IRequest<CreatePaymentResponse>;

public sealed record CreatePaymentResponse(
    Guid PaymentId,
    string Status,
    string Reference,
    DateTime CreatedAt
);

/// <summary>
/// Complete payment command - Webhook callback
/// </summary>
public sealed record CompletePaymentCommand(string Reference) : IRequest<CompletePaymentResponse>;

public sealed record CompletePaymentResponse(
    Guid PaymentId,
    string Status,
    DateTime CompletedAt
);

/// <summary>
/// Reconciliation command - Generate report
/// </summary>
public sealed record ReconcilePaymentsCommand : IRequest<ReconciliationReportResponse>;

public sealed record ReconciliationReportResponse(
    int TotalPayments,
    int PendingPayments,
    int CompletedPayments,
    int FailedPayments,
    decimal CustomerBalance,
    decimal ClearingBalance,
    decimal MerchantBalance,
    bool IsBalanced,
    DateTime GeneratedAt
);
