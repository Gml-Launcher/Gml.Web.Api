﻿using System.Collections.Frozen;
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
                .Matches("^[a-zA-Z0-9- ]*$").WithMessage("Имя может содержать только английские буквы, цифры, пробелы и тире.")
                .Length(2, 100).WithMessage("Длина имени профиля должна быть от 2 до 100 символов.");

            child.RuleFor(x => x.Path)
                .NotEmpty().WithMessage("Путь обязателен.");
        });
    }
}
