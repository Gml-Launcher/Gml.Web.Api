using FluentValidation;
using Gml.Web.Api.Dto.User;

namespace Gml.Web.Api.Core.Validation;

public class UserCreateValidationFilter : AbstractValidator<UserCreateDto>
{
    public UserCreateValidationFilter()
    {
        RuleFor(c => c.Login)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Пожалуйста, укажите login.")
            .Length(3, 50).WithMessage("Login должен быть от 3 до 50 символов.")
            .Matches("^[a-zA-Z0-9]*$").WithMessage("Login должен состоять только из букв и цифр.");

        RuleFor(c => c.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Пожалуйста, укажите пароль.")
            .Length(5, 100).WithMessage("Пароль должен быть от 5 до 100 символов.")
            .Must(ContainAtLeastOneUppercaseLetter).WithMessage("Пароль должен содержать хотя бы одну заглавную букву.")
            .Must(ContainAtLeastOneLowercaseLetter).WithMessage("Пароль должен содержать хотя бы одну строчную букву.")
            .Must(ContainAtLeastOneNumber).WithMessage("Пароль должен содержать хотя бы одну цифру.");

        RuleFor(c => c.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Пожалуйста, укажите адрес электронной почты.")
            .EmailAddress().WithMessage("Пожалуйста, укажите действующий адрес электронной почты.");
    }

    private bool ContainAtLeastOneUppercaseLetter(string password)
    {
        return password.Any(char.IsUpper);
    }

    private bool ContainAtLeastOneLowercaseLetter(string password)
    {
        return password.Any(char.IsLower);
    }

    private bool ContainAtLeastOneNumber(string password)
    {
        return password.Any(char.IsDigit);
    }
}
