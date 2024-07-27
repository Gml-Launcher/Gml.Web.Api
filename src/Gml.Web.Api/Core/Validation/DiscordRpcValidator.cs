using FluentValidation;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Validation;

public class DiscordRpcValidator : AbstractValidator<DiscordRpcUpdateDto>
{
    public DiscordRpcValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ClientId обязателен для заполнения")
            .Length(1, 32).WithMessage("ClientId должен содержать от 1 до 32 символов");

        RuleFor(x => x.Details)
            .MaximumLength(128).WithMessage("Details не может быть длинее 128 символов");

        RuleFor(x => x.LargeImageKey)
            .MaximumLength(32).WithMessage("LargeImageKey не может быть длинее 32 символов");

        RuleFor(x => x.LargeImageText)
            .MaximumLength(128).WithMessage("LargeImageText не может быть длинее 128 символов");

        RuleFor(x => x.SmallImageKey)
            .MaximumLength(32).WithMessage("SmallImageKey не может быть длинее 32 символов");

        RuleFor(x => x.SmallImageText)
            .MaximumLength(128).WithMessage("SmallImageText не может быть длинее 128 символов");
    }
}
