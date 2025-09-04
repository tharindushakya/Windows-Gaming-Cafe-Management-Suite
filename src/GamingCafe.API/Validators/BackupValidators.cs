using FluentValidation;
using GamingCafe.Core.DTOs;

namespace GamingCafe.API.Validators;

public class CreateBackupRequestValidator : AbstractValidator<CreateBackupRequest>
{
    public CreateBackupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Backup name is required")
            .MinimumLength(3).WithMessage("Backup name must be at least 3 characters long")
            .MaximumLength(50).WithMessage("Backup name must not exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Backup name can only contain letters, numbers, underscores, and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid backup type");

        RuleFor(x => x)
            .Must(x => x.IncludeData || x.IncludeSchema)
            .WithMessage("At least one of 'Include Data' or 'Include Schema' must be selected");
    }
}

public class RestoreBackupRequestValidator : AbstractValidator<RestoreBackupRequest>
{
    public RestoreBackupRequestValidator()
    {
        RuleFor(x => x.BackupName)
            .NotEmpty().WithMessage("Backup name is required")
            .MinimumLength(3).WithMessage("Backup name must be at least 3 characters long")
            .MaximumLength(50).WithMessage("Backup name must not exceed 50 characters");

        RuleFor(x => x.ConfirmRestore)
            .Equal(true).WithMessage("Restore confirmation is required for this dangerous operation");
    }
}

public class BackupScheduleRequestValidator : AbstractValidator<BackupScheduleRequest>
{
    public BackupScheduleRequestValidator()
    {
        RuleFor(x => x.Interval)
            .Must(interval => interval.TotalMinutes >= 60)
            .WithMessage("Backup interval must be at least 1 hour");

        RuleFor(x => x.RetentionDays)
            .GreaterThan(0).WithMessage("Retention days must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Retention days must not exceed 365 days");

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid backup type");
    }
}
