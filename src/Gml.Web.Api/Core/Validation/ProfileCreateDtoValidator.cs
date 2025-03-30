using FluentValidation;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Validation;

public class ProfileCreateDtoValidator : AbstractValidator<ProfileCreateDto>
{
    public ProfileCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно.")
            .Matches("^[a-zA-Z0-9-]*$").WithMessage("Название профиля может содержать только английские буквы, цифры и тире.")
            .Length(2, 100).WithMessage("Длина имени должна быть от 2 до 100 символов.");
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Отображаемое имя обязательно.")
            .Length(2, 100).WithMessage("Длина имени должна быть от 2 до 100 символов.");
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Описание обязательно.")
            .Length(2, 1000).WithMessage("Длина описания должна быть от 2 до 1000 символов.");
        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Версия обязательна.")
            .Length(2, 100).WithMessage("Длина версии должна быть от 2 до 100 символов.");
    }
}
