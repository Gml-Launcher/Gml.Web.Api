using FluentValidation;
using Gml.Web.Api.Dto.Mods;
using Gml.Web.Api.Dto.User;

namespace Gml.Web.Api.Core.Validation;

public class ModsUpdateInfoValidator : AbstractValidator<ModsDetailsInfoDto>
{
    public ModsUpdateInfoValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Поле Key не должно быть пустым.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Поле Title не должно быть пустым.")
            .MaximumLength(100).WithMessage("Поле Title не должно превышать 100 символов.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Поле Description не должно быть пустым.");
    }
}
