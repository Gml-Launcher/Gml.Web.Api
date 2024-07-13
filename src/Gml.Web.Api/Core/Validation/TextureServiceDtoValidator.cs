using FluentValidation;
using Gml.Web.Api.Dto.Texture;

namespace Gml.Web.Api.Core.Validation;

public class TextureServiceDtoValidator : AbstractValidator<UrlServiceDto>
{
    public TextureServiceDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL обязателен.")
            .Must(ValidateUrl).WithMessage("Невалидный URL.");
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
