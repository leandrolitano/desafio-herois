using FluentValidation;

namespace Herois.Application.Herois.Validators;

public class DeleteHeroCommandValidator : AbstractValidator<DeleteHeroCommand>
{
    public DeleteHeroCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id deve ser maior que 0");
    }
}
