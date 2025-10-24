using AutoMapper;
using Gml.Dto.Files;
using Gml.Models.System;

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
