using FluentValidation;
using GamingCafe.API.Controllers;

namespace GamingCafe.API.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).MaximumLength(50);

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().When(x => !string.IsNullOrEmpty(x.Email)).WithMessage("Email is required when provided")
            .EmailAddress().MaximumLength(255);

        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MinimumLength(7).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        RuleFor(x => x.InitialWalletBalance).GreaterThanOrEqualTo(0).When(x => x.InitialWalletBalance.HasValue);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
    RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
    RuleFor(x => x.Email).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MinimumLength(7).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
