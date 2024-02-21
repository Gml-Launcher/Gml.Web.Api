using AutoMapper;
using Gml.Core.Launcher;
using Gml.Core.System;
using Gml.Models;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Profiles;

public class ProfileMapper : Profile
{
    public ProfileMapper()
    {
        CreateMap<GameProfile, ProfileReadDto>();
        CreateMap<GameProfileInfo, ProfileReadInfoDto>();
        CreateMap<LocalFileInfo, ProfileFileReadDto>();
    }
}