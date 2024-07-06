using FluentValidation;
using Gml.Web.Api.Dto.User;
using Spectre.Console;

namespace Gml.Web.Api.Core.Validation;

public class PlayerAuthDtoValidator : AbstractValidator<BaseUserPassword>
{
    public PlayerAuthDtoValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Поле логина обязательно для заполнения.")
            .Length(3, 50).WithMessage("Логин должен содержать от 3 до 50 символов.");
    }
}
