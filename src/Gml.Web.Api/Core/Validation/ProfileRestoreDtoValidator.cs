using FluentValidation;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Validation;

public class ProfileRestoreDtoValidator : AbstractValidator<ProfileRestoreDto>
{
    public ProfileRestoreDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно.")
            .Matches("^[a-zA-Z0-9-]*$").WithMessage("Название профиля может содержать только английские буквы, цифры и тире.")
            .Length(2, 100).WithMessage("Длина имени должна быть от 2 до 100 символов.");
    }
}
