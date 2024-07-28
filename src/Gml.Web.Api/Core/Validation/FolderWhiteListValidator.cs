using FluentValidation;
using Gml.Web.Api.Dto.Files;

namespace Gml.Web.Api.Core.Validation;

public class FolderWhiteListValidator : AbstractValidator<List<FolderWhiteListDto>>
{
    public FolderWhiteListValidator()
    {
        RuleForEach(x => x).ChildRules(child =>
        {
            child.RuleFor(x => x.ProfileName)
                .NotEmpty().WithMessage("Имя профиля обязательно.")
                .Matches("^[a-zA-Z0-9]*$").WithMessage("Имя профиля может содержать только английские буквы и цифры.")
                .Length(2, 100).WithMessage("Длина имени профиля должна быть от 2 до 100 символов.");

            child.RuleFor(x => x.Path)
                .NotEmpty().WithMessage("Путь обязателен.")
                .Matches("^[a-zA-Z0-9]*$")
                .WithMessage("Путь может содержать только английские буквы и цифры.")
                .Length(2, 100).WithMessage("Длина пути должна быть от 2 до 100 символов.");
        });
    }
}
