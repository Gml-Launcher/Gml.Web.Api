using Mapster;
using Gml.Dto.Files;
using Gml.Models.System;

namespace Gml.Web.Api.Core.MappingProfiles;

public class SystemIOMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<LocalFileInfo, ProfileFileReadDto>();
        config.NewConfig<LocalFolderInfo, ProfileFolderReadDto>();
        config.NewConfig<FolderWhiteListDto, LocalFolderInfo>();
    }
}
