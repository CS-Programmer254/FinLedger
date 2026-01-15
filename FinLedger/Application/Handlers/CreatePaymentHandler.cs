namespace FinLedger.Application.Handlers;

using FinLedger.Application.Commands;
using FinLedger.Application.Interfaces;
using FinLedger.Domain.Aggregates;
using FinLedger.Domain.StronglyTypedIds;
using FinLedger.Domain.ValueObjects;
using FinLedger.Infrastructure.Persistence;
using MediatR;

/// <summary>
/// Create Payment Handler
/// Demonstrates aggregate root creation and event publishing
/// </summary>
public sealed class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEventStore _eventStore;
    private readonly ILogger<CreatePaymentHandler> _logger;

    public CreatePaymentHandler(
        IPaymentRepository paymentRepository,
        IEventStore eventStore,
        ILogger<CreatePaymentHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _eventStore = eventStore;
        _logger = logger;
    }

    public async Task<CreatePaymentResponse> Handle(
        CreatePaymentCommand cmd,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating payment - Merchant: {MerchantId}, Reference: {Reference}",
            cmd.MerchantId, cmd.Reference);

        // Check Idempotency 
        var existingPayment = await _paymentRepository.GetByReferenceAsync(cmd.Reference);
        if (existingPayment != null)
        {
            _logger.LogWarning("Duplicate payment attempt - Reference: {Reference}", cmd.Reference);
            return new CreatePaymentResponse(
                existingPayment.Id,
                existingPayment.Status.ToString(),
                existingPayment.Reference.Value,
                existingPayment.CreatedAt);
        }

        var merchantId = new MerchantId(cmd.MerchantId);
        var money = new Money(cmd.Amount, cmd.Currency);
        var reference = new PaymentReference(cmd.Reference);

        var payment = new Payment(merchantId, money, reference, cmd.WebhookUrl);

        //Reserve Funds 
        //This is where the ledger entries are created
        payment.ReserveFunds();
        await _paymentRepository.AddAsync(payment);

        foreach (var @event in payment.DomainEvents)
        {
            await _eventStore.AppendAsync(payment.Id, @event);
            _logger.LogInformation(
                "Event published: {EventType} for Payment {PaymentId}",
                @event.GetType().Name, payment.Id);
        }

        _logger.LogInformation("Payment created - PaymentId: {PaymentId}", payment.Id);

        return new CreatePaymentResponse(
            payment.Id,
            payment.Status.ToString(),
            payment.Reference.Value,
            payment.CreatedAt);
    }
}




