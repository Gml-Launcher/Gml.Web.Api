using FluentValidation;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Validation;

public class ProfileCreateInfoDtoValidator : AbstractValidator<ProfileCreateInfoDto>
{
    public ProfileCreateInfoDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Имя пользователя обязательно.")
            .Length(2, 100).WithMessage("Длина имени пользователя должна быть от 2 до 100 символов.");
        RuleFor(x => x.UserAccessToken)
            .NotEmpty().WithMessage("Токен пользователя обязателен.")
            .Length(2, 1000).WithMessage("Длина токена пользователя должна быть от 2 до 1000 символов.");
        RuleFor(x => x.ProfileName)
            .NotEmpty().WithMessage("Название клиента обязательно.")
            .Length(2, 100).WithMessage("Длина названия клиента должна быть от 2 до 100 символов.");
        RuleFor(x => x.UserUuid)
            .NotEmpty().WithMessage("UserUUID обязателен.")
            .Length(2, 100).WithMessage("Длина UserUUID должна быть от 2 до 100 символов.");
        RuleFor(x => x.OsArchitecture)
            .NotEmpty().WithMessage("Архитектура ОС обязательна.")
            .Length(2, 100).WithMessage("Длина архитектуры ОС должна быть от 2 до 100 символов.");
        RuleFor(x => x.OsType);
    }
}
