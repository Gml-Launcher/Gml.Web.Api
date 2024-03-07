using Gml.Web.Api.Domains.Texture;

namespace Gml.Web.Api.Core.Validation;

using System;
using FluentValidation;

public class TextureServiceDtoValidator : AbstractValidator<TextureServiceDto>
{
    public TextureServiceDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL обязателен.")
            .Must(ValidateUrl).WithMessage("Невалидный URL.")
            .Must(ContainUserName).WithMessage("В адресной строке отсутствует обязательный атрибут {userName}, который необходим для замены текстуры пользователей.");
    }

    private bool ValidateUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private bool ContainUserName(string? url)
    {
        return url?.Contains("{userName}") ?? false;
    }
}
