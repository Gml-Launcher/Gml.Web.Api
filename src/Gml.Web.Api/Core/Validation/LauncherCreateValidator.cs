using FluentValidation;
using Gml.Web.Api.Dto.Launcher;

namespace Gml.Web.Api.Core.Validation;

public class LauncherCreateDtoValidator : AbstractValidator<LauncherCreateDto>
{
    public LauncherCreateDtoValidator()
    {
        RuleFor(x => x.GitHubVersions)
            .NotEmpty().WithMessage("Поле GitHubVersions обязательно для заполнения.")
            .Length(3, 50).WithMessage("GitHubVersions должен содержать от 3 до 50 символов.");

        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("Поле Host обязательно для заполнения.")
            .Length(3, 50).WithMessage("Host должен содержать от 3 до 50 символов.");

        RuleFor(x => x.Folder)
            .NotEmpty().WithMessage("Поле Folder обязательно для заполнения.")
            .Length(3, 50).WithMessage("Folder должен содержать от 3 до 50 символов.")
            .Matches(@"^[^\\\/:*?""<>|]+$")
            .WithMessage("Folder не должно содержать недопустимых символов: \\ / : * ? \" < > |");
    }
}
