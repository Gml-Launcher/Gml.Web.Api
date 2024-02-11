using FluentValidation;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Handlers;

public class ProfileHandler : IProfileHandler
{
    public static Task<IResult> GetProfiles()
    {
        throw new NotImplementedException();
    }

    public static Task<IResult> CreateProfile(IValidator<ProfileCreateDto> validator, ProfileCreateDto createDto)
    {
        throw new NotImplementedException();
    }

    public static Task<IResult> UpdateProfile(IValidator<ProfileUpdateDto> validator, ProfileUpdateDto updateDto)
    {
        throw new NotImplementedException();
    }

    public static Task<IResult> RestoreProfile(IValidator<ProfileRestoreDto> validator, ProfileRestoreDto profileName)
    {
        throw new NotImplementedException();
    }

    public static Task<IResult> CompileProfile(IValidator<CompileProfileDto> validator, CompileProfileDto profileName)
    {
        throw new NotImplementedException();
    }

    public static Task<IResult> GetProfileInfo(IValidator<ProfileCreateInfoDto> validator, ProfileCreateInfoDto createInfoDto)
    {
        throw new NotImplementedException();
    }

    public static Task<IResult> RemoveProfile(string profileName)
    {
        throw new NotImplementedException();
    }
}