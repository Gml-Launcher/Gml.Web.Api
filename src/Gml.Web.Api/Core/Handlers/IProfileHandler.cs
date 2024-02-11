using FluentValidation;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Handlers;

public interface IProfileHandler
{
    static abstract Task<IResult> GetProfiles();
    
    static abstract Task<IResult> CreateProfile(
        IValidator<ProfileCreateDto> validator, 
        ProfileCreateDto createDto);
    
    static abstract Task<IResult> UpdateProfile(
        IValidator<ProfileUpdateDto> validator,
        ProfileUpdateDto updateDto);
    
    static abstract Task<IResult> RestoreProfile(
        IValidator<ProfileRestoreDto> validator, 
        ProfileRestoreDto profileName);
    
    static abstract Task<IResult> CompileProfile(
        IValidator<CompileProfileDto> validator, 
        CompileProfileDto profileName);
    
    static abstract Task<IResult> GetProfileInfo(
        IValidator<ProfileCreateInfoDto> validator, 
        ProfileCreateInfoDto createInfoDto);
    
    static abstract Task<IResult> RemoveProfile(string profileName);
}