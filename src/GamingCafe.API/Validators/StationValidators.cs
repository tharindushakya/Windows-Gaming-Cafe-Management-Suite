using FluentValidation;
using GamingCafe.API.Controllers;

namespace GamingCafe.API.Validators;

public class CreateGameStationRequestValidator : AbstractValidator<CreateGameStationRequest>
{
    public CreateGameStationRequestValidator()
    {
        RuleFor(x => x.StationName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StationType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HourlyRate).GreaterThan(0).LessThanOrEqualTo(999.99m);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class UpdateGameStationRequestValidator : AbstractValidator<UpdateGameStationRequest>
{
    public UpdateGameStationRequestValidator()
    {
        RuleFor(x => x.StationName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StationType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HourlyRate).GreaterThan(0).LessThanOrEqualTo(999.99m);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class SetStationAvailabilityRequestValidator : AbstractValidator<SetStationAvailabilityRequest>
{
    public SetStationAvailabilityRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(200);
    }
}
