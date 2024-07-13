using FluentValidation;
using Gml.Web.Api.Domains.Servers;

namespace Gml.Web.Api.Core.Validation;

public class CreateServerDtoValidator : AbstractValidator<CreateServerDto>
{
    public CreateServerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя сервера обязательно.")
            .Length(3, 100).WithMessage("Длина имени сервера должна быть от 3 до 100 символов.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Адрес сервера обязателен.")
            .Matches(@"^([a-zA-Z0-9\.\-]+)$").WithMessage("Адрес сервера может содержать только английские буквы, цифры, точки и дефисы.")
            .Length(5, 255).WithMessage("Длина адреса сервера должна быть от 5 до 255 символов.");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535).WithMessage("Порт должен быть в диапазоне от 1 до 65535.");
    }
}