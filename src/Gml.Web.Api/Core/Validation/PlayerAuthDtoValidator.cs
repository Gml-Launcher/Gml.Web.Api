using Gml.Web.Api.Dto.User;

namespace Gml.Web.Api.Core.Validation;

using FluentValidation;

public class PlayerAuthDtoValidator : AbstractValidator<BaseUserPassword>
{
    public PlayerAuthDtoValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Поле логина обязательно для заполнения.")
            .Length(3, 50).WithMessage("Логин должен содержать от 3 до 50 символов."); 
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Поле пароля обязательно для заполнения.")
            .Length(6, 100).WithMessage("Пароль должен содержать от 6 до 100 символов.");
    }
}