using FluentValidation;

namespace Herois.Application.Herois.Validators;

public class GetHeroByIdQueryValidator : AbstractValidator<GetHeroByIdQuery>
{
    public GetHeroByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id deve ser maior que 0");
    }
}
