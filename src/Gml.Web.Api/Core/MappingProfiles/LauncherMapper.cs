using Mapster;
using Gml.Dto.Launcher;
using GmlCore.Interfaces.Launcher;

namespace Gml.Web.Api.Core.MappingProfiles;

public class LauncherMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ILauncherBuild, LauncherBuildReadDto>();
    }
}
