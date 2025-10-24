using AutoMapper;
using Gml.Core.Launcher;
using Gml.Dto.Profile;
using Gml.Dto.Servers;
using Gml.Models;
using Gml.Models.Servers;
using Gml.Models.System;

namespace Gml.Web.Api.Core.MappingProfiles;

public class ProfileMapper : Profile
{
    public ProfileMapper()
    {
        CreateMap<MinecraftServer, ServerReadDto>();
        CreateMap<GameProfile, ProfileReadDto>();
        CreateMap<GameProfileInfo, ProfileReadInfoDto>();
    }
}
