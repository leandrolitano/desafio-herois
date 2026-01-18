using FluentValidation;

namespace Herois.Application.Herois.Validators;

public class UpdateHeroCommandValidator : AbstractValidator<UpdateHeroCommand>
{
    public UpdateHeroCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id deve ser maior que 0");

        RuleFor(x => x.Nome)
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Nome e obrigatorio")
            .MaximumLength(120);

        RuleFor(x => x.NomeHeroi)
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("NomeHeroi e obrigatorio")
            .MaximumLength(120);

        RuleFor(x => x.DataNascimento)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("DataNascimento nao pode ser futura");

        RuleFor(x => x.Altura)
            .GreaterThan(0).WithMessage("Altura deve ser maior que 0");

        RuleFor(x => x.Peso)
            .GreaterThan(0).WithMessage("Peso deve ser maior que 0");

        RuleFor(x => x.SuperpoderIds)
            .NotNull()
            .Must(list => list.Count > 0).WithMessage("Selecione ao menos 1 superpoder");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv.Length > 0).WithMessage("RowVersion e obrigatorio para atualizacao");
    }
}
