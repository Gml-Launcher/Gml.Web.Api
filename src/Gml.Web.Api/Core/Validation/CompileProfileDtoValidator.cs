using FluentValidation;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Validation;

public class CompileProfileDtoValidator : AbstractValidator<CompileProfileDto>
{
    public CompileProfileDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно.")
            .Matches("^[a-zA-Z0-9]*$").WithMessage("Имя может содержать только английские буквы и цифры.")
            .Length(2, 100).WithMessage("Длина имени должна быть от 2 до 100 символов.");
    }
}