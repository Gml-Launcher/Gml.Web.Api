using FluentValidation;
using Gml.Web.Api.Core.Handlers;

namespace Gml.Web.Api.Core.Validation;

public class SettingsInstallRecordValidator : AbstractValidator<SettingsHandler.SettingsInstallRecord>
{
    public SettingsInstallRecordValidator()
    {
        RuleFor(x => x.ProjectName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Название проекта обязательно.")
            .Length(2, 100).WithMessage("Длина названия проекта должна быть от 2 до 100 символов.")
            .Matches("^[a-zA-Z]*$").WithMessage("Название проекта может содержать только английские буквы без пробелов.");

        RuleFor(x => x.BackendAddress)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Адрес backend обязателен.")
            .Must(BeValidUrl).WithMessage("Адрес backend должен быть валидным URL (например, https://example.com).");

        RuleFor(x => x.AdminUsername)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Имя администратора обязательно.")
            .Length(3, 50).WithMessage("Имя администратора должно быть от 3 до 50 символов.")
            .Matches("^[a-zA-Z0-9._-]*$").WithMessage("Имя администратора может содержать только буквы, цифры, точки, тире и подчёркивания.");

        RuleFor(x => x.AdminPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Пароль администратора обязателен.")
            .Length(5, 100).WithMessage("Пароль должен быть от 5 до 100 символов.")
            .Must(ContainAtLeastOneUppercaseLetter).WithMessage("Пароль должен содержать хотя бы одну заглавную букву.")
            .Must(ContainAtLeastOneLowercaseLetter).WithMessage("Пароль должен содержать хотя бы одну строчную букву.")
            .Must(ContainAtLeastOneNumber).WithMessage("Пароль должен содержать хотя бы одну цифру.");

        RuleFor(x => x.ConfirmPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Подтверждение пароля обязательно.")
            .Equal(x => x.AdminPassword).WithMessage("Пароли не совпадают.");
    }

    private bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private bool ContainAtLeastOneUppercaseLetter(string password) => password.Any(char.IsUpper);
    private bool ContainAtLeastOneLowercaseLetter(string password) => password.Any(char.IsLower);
    private bool ContainAtLeastOneNumber(string password) => password.Any(char.IsDigit);
}
