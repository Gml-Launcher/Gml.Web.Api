using GmlCore.Interfaces.System;

namespace Gml.Web.Api.Dto.Files;

public class FolderWhiteListDto : IFolderInfo
{
    public string ProfileName { get; set; }
    public string Path { get; set; }
}
