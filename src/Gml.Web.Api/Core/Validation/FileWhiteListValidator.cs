using FluentValidation;
using Gml.Web.Api.Dto.Files;

namespace Gml.Web.Api.Core.Validation;

public class FileWhiteListValidator : AbstractValidator<List<FileWhiteListDto>>
{
    public FileWhiteListValidator()
    {
        RuleForEach(x => x).ChildRules(child =>
        {
            child.RuleFor(x => x.ProfileName)
                .NotEmpty().WithMessage("Имя профиля обязательно.")
                .Matches("^[a-zA-Z0-9]*$").WithMessage("Имя профиля может содержать только английские буквы и цифры.")
                .Length(2, 100).WithMessage("Длина имени профиля должна быть от 2 до 100 символов.");

            child.RuleFor(x => x.Hash)
                .NotEmpty().WithMessage("Хэш обязателен.")
                .Matches("^[a-fA-F0-9]*$")
                .WithMessage("Хэш может содержать только HEX символы (цифры от 0 до 9 и буквы от A до F).");
        });
    }
}
