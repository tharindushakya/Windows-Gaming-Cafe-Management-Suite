using FluentValidation;
using GamingCafe.API.Controllers;

namespace GamingCafe.API.Validators;

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.StationId).GreaterThan(0);
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("StartTime must be before EndTime");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class CreateLoyaltyProgramRequestValidator : AbstractValidator<CreateLoyaltyProgramRequest>
{
    public CreateLoyaltyProgramRequestValidator()
    {
        RuleFor(x => x.ProgramName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PointsPerDollar).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
