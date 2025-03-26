using FluentValidation;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Validation;

public class ProfileUpdateDtoValidator : AbstractValidator<ProfileUpdateDto>
{
    public ProfileUpdateDtoValidator()
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
            .Length(2, 255).WithMessage("Длина описания должна быть от 2 до 255 символов.");
        RuleFor(x => x.OriginalName)
            .NotEmpty().WithMessage("Оригинальное имя обязательно.")
            .Length(2, 100).WithMessage("Длина оригинального имени должна быть от 2 до 100 символов.");
    }
}
