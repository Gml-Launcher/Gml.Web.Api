using AutoMapper;
using Gml.Web.Api.Dto.Launcher;
using GmlCore.Interfaces.Launcher;

namespace Gml.Web.Api.Core.MappingProfiles;

public class LauncherMapper : Profile
{
    public LauncherMapper()
    {
        CreateMap<ILauncherBuild, LauncherBuildReadDto>();
    }
}