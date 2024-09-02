using AutoMapper;
using Gml.Models.System;
using Gml.Web.Api.Dto.Files;

namespace Gml.Web.Api.Core.MappingProfiles;

public class SystemIOMapper : Profile
{
    public SystemIOMapper()
    {
        CreateMap<LocalFileInfo, ProfileFileReadDto>();
        CreateMap<LocalFolderInfo, ProfileFolderReadDto>();
        CreateMap<FolderWhiteListDto, LocalFolderInfo>();
    }
}
