namespace FinLedger.Api.Controllers;
using Asp.Versioning;
using FinLedger.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Payments Controller - Handles payment creation and queries
/// API Version: v1
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/payments")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment
    /// </summary>
    /// <param name="cmd">Payment creation command</param>
    /// <returns>Created payment</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("POST /api/v1/payments - Creating payment for merchant {MerchantId}",
            request.MerchantId);

        var cmd = new CreatePaymentCommand(
            request.MerchantId,
            request.Amount,
            request.Currency,
            request.Reference,
            request.WebhookUrl
        );

        var response = await _mediator.Send(cmd, cancellationToken);

        return CreatedAtAction(nameof(GetPayment),
            new { id = response.PaymentId },
            response);
    }

    /// <summary>
    /// Get payment by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        // Implementation: Query payment repository
        return Ok(new { id, status = "PENDING" });
    }

    /// <summary>
    /// Get payment history
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50)
    {
        // Implementation: Query with pagination
        return Ok(new[] { new { id = Guid.NewGuid(), status = "PENDING" } });
    }
}

public sealed record CreatePaymentRequest(
    [Required] Guid MerchantId,
    [Range(1, int.MaxValue)] int Amount,
    [StringLength(3, MinimumLength = 3)] string Currency,
    [StringLength(50, MinimumLength = 5)] string Reference,
    [Url] string? WebhookUrl = null
);

/// <summary>
/// Webhook Controller - Handles external payment confirmations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/webhooks")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IMediator mediator, ILogger<WebhookController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Payment gateway callback webhook
    /// Idempotent operation - safe to retry
    /// </summary>
    [HttpPost("callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PaymentCallback(
        [FromBody] WebhookCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("POST /api/v1/webhooks/callback - Reference: {Reference}", request.Reference);

        var cmd = new CompletePaymentCommand(request.Reference);

        try
        {
            var response = await _mediator.Send(cmd, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Payment not found: {Reference}", request.Reference);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Webhook health check
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}

public sealed record WebhookCallbackRequest(
    [StringLength(50)] string Reference
);

/// <summary>
/// Reconciliation Controller - Financial reporting
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/reconciliation")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class ReconciliationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReconciliationController> _logger;

    public ReconciliationController(IMediator mediator, ILogger<ReconciliationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generate reconciliation report
    /// Shows ledger balances and payment status summary
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReconciliation(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GET /api/v1/reconciliation - Generating report");

        var cmd = new ReconcilePaymentsCommand();
        var report = await _mediator.Send(cmd, cancellationToken);

        return Ok(report);
    }
}
