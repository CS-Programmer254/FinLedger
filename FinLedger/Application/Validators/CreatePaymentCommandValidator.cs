using FinLedger.Application.Commands;
using FluentValidation;

namespace FinLedger.Application.Validators;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("Merchant ID required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive")
            .LessThan(1_000_000_000).WithMessage("Amount too large");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency required")
            .Length(3).WithMessage("Currency must be 3 characters (ISO 4217)");

        RuleFor(x => x.Reference)
            .NotEmpty().WithMessage("Reference required")
            .MaximumLength(50).WithMessage("Reference max 50 characters");

        RuleFor(x => x.WebhookUrl)
            .Must(x => x == null || Uri.TryCreate(x, UriKind.Absolute, out _))
            .WithMessage("Webhook URL must be valid");
    }
}

public sealed class CompletePaymentCommandValidator : AbstractValidator<CompletePaymentCommand>
{
    public CompletePaymentCommandValidator()
    {
        RuleFor(x => x.Reference)
            .NotEmpty().WithMessage("Reference required")
            .MaximumLength(50).WithMessage("Reference max 50 characters");
    }
}