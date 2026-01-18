using FluentValidation;

namespace Herois.Application.Herois.Validators;

public class GetHeroesQueryValidator : AbstractValidator<GetHeroesQuery>
{
    public GetHeroesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page deve ser maior que 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200).WithMessage("PageSize deve estar entre 1 e 200");
    }
}
