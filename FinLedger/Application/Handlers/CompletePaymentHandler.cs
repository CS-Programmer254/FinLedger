using FinLedger.Application.Commands;
using FinLedger.Application.Events;
using FinLedger.Application.Interfaces;
using FinLedger.Domain.Aggregates;
using FinLedger.Domain.Enums;
using FinLedger.Domain.ValueObjects;
using MediatR;

namespace FinLedger.Application.Handlers;

/// <summary>
/// Complete Payment Handler
/// Demonstrates aggregate root modification and webhook management
/// </summary>
public sealed class CompletePaymentHandler : IRequestHandler<CompletePaymentCommand, CompletePaymentResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IWebhookRepository _webhookRepository;
    private readonly IEventStore _eventStore;
    private readonly IPublisher _publisher;
    private readonly ILogger<CompletePaymentHandler> _logger;

    public CompletePaymentHandler(
        IPaymentRepository paymentRepository,
        IWebhookRepository webhookRepository,
        IEventStore eventStore,
        IPublisher publisher,
        ILogger<CompletePaymentHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _webhookRepository = webhookRepository;
        _eventStore = eventStore;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CompletePaymentResponse> Handle(
        CompletePaymentCommand cmd,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing payment - Reference: {Reference}", cmd.Reference);

        // Only load the aggregate root, not individual entities
        var payment = await _paymentRepository.GetByReferenceAsync(cmd.Reference)
            ?? throw new KeyNotFoundException($"Payment not found: {cmd.Reference}");

        // Check Idempotency
        if (payment.Status == PaymentStatus.Completed)
        {
            _logger.LogWarning("Payment already completed - PaymentId: {PaymentId}", payment.Id);
            return new CompletePaymentResponse(
                payment.Id,
                payment.Status.ToString(),
                payment.CompletedAt!.Value);
        }

        try
        {
            payment.MarkCompleted(); // creates ledger entries
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Cannot complete payment - PaymentId: {PaymentId}", payment.Id);
            throw;
        }

        await _paymentRepository.UpdateAsync(payment);

        //Publish Domain Events
        foreach (var @event in payment.DomainEvents)
        {
            await _eventStore.AppendAsync(payment.Id, @event);
            _logger.LogInformation(
                "Event published: {EventType} for Payment {PaymentId}",
                @event.GetType().Name, payment.Id);
        }

        //Setup Webhook Delivery 
        if (!string.IsNullOrEmpty(payment.WebhookUrl))
        {
            // Create webhook aggregate
            var webhookAggregate = new WebhookAggregate(payment.Id);

            // Prepare encrypted payload
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                paymentId = payment.Id,
                status = "COMPLETED",
                reference = payment.Reference.Value,
                amount = payment.Amount.Amount,
                currency = payment.Amount.Currency,
                completedAt = payment.CompletedAt
            });

            var encryptedPayload = EncryptedPayload.Encrypt(payload, "your-secret-key");

            // Add delivery tracking
            webhookAggregate.AddDelivery(payment.WebhookUrl,
                System.Text.Json.JsonSerializer.Serialize(encryptedPayload));

            // Persist webhook aggregate
            await _webhookRepository.AddAsync(webhookAggregate);

            // Publish webhook event
            await _publisher.Publish(
                new WebhookNotificationEvent(webhookAggregate.Id, payment.WebhookUrl, payload),
                cancellationToken);

            _logger.LogInformation(
                "Webhook queued for delivery - WebhookId: {WebhookId}, PaymentId: {PaymentId}",
                webhookAggregate.Id, payment.Id);
        }

        _logger.LogInformation("Payment completed - PaymentId: {PaymentId}", payment.Id);

        return new CompletePaymentResponse(
            payment.Id,
            payment.Status.ToString(),
            payment.CompletedAt!.Value);
    }
}


