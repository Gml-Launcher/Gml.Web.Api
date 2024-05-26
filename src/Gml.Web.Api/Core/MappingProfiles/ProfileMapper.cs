using AutoMapper;
using Gml.Core.Launcher;
using Gml.Core.System;
using Gml.Models;
using Gml.Models.Servers;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Profile;
using Gml.Web.Api.Dto.Servers;

namespace Gml.Web.Api.Core.MappingProfiles;

public class ProfileMapper : Profile
{
    public ProfileMapper()
    {
        CreateMap<MinecraftServer, ServerReadDto>();
        CreateMap<GameProfile, ProfileReadDto>();
        CreateMap<GameProfileInfo, ProfileReadInfoDto>();
        CreateMap<LocalFileInfo, ProfileFileReadDto>();
    }
}
