using FluentValidation;
using Gml.Web.Api.Dto.Integration;

namespace Gml.Web.Api.Core.Validation;

public class IntegrationValidator : AbstractValidator<IntegrationUpdateDto>
{
    public IntegrationValidator()
    {
        RuleFor(x => x.Endpoint)
            .NotEmpty().WithMessage("Endpoint обязателен.")
            .Must(IsValidUrl).WithMessage("Endpoint должен быть валидным URL.");
    }

    private bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
