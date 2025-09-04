using FluentValidation;
using GamingCafe.Core.DTOs;

namespace GamingCafe.API.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Access token is required");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}

public class PasswordResetRequestValidator : AbstractValidator<PasswordResetRequest>
{
    public PasswordResetRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
    }
}

public class PasswordResetConfirmRequestValidator : AbstractValidator<PasswordResetConfirmRequest>
{
    public PasswordResetConfirmRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character");
    }
}

public class EmailVerificationRequestValidator : AbstractValidator<EmailVerificationRequest>
{
    public EmailVerificationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
    }
}

public class EmailVerificationConfirmRequestValidator : AbstractValidator<EmailVerificationConfirmRequest>
{
    public EmailVerificationConfirmRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required");
    }
}

public class TwoFactorSetupRequestValidator : AbstractValidator<TwoFactorSetupRequest>
{
    public TwoFactorSetupRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long");
    }
}

public class TwoFactorVerifyRequestValidator : AbstractValidator<TwoFactorVerifyRequest>
{
    public TwoFactorVerifyRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Code) || !string.IsNullOrEmpty(x.RecoveryCode))
            .WithMessage("Either verification code or recovery code is required");

        When(x => !string.IsNullOrEmpty(x.Code), () =>
        {
            RuleFor(x => x.Code)
                .Length(6).WithMessage("Verification code must be 6 digits")
                .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits");
        });

        When(x => !string.IsNullOrEmpty(x.RecoveryCode), () =>
        {
            RuleFor(x => x.RecoveryCode)
                .Matches(@"^[a-f0-9]{5}-[a-f0-9]{5}$").WithMessage("Recovery code format is invalid");
        });
    }
}

public class TwoFactorDisableRequestValidator : AbstractValidator<TwoFactorDisableRequest>
{
    public TwoFactorDisableRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long");
    }
}
