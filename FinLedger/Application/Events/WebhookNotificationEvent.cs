using MediatR;

namespace FinLedger.Application.Events;

/// <summary>
/// Webhook notification event - Published when payment completes
/// </summary>
public sealed record WebhookNotificationEvent(
    Guid WebhookId,
    string Url,
    string Payload) : INotification;
