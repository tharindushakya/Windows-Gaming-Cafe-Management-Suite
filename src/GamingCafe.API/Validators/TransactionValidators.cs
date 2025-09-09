using FluentValidation;
using GamingCafe.API.Controllers;

namespace GamingCafe.API.Validators;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.PaymentReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class UpdateTransactionStatusRequestValidator : AbstractValidator<UpdateTransactionStatusRequest>
{
    public UpdateTransactionStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class ProcessRefundRequestValidator : AbstractValidator<ProcessRefundRequest>
{
    public ProcessRefundRequestValidator()
    {
        RuleFor(x => x.RefundAmount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}
