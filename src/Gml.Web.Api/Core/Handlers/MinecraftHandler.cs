using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Dto.Minecraft.AuthLib;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Handlers;

public class MinecraftHandler : IMinecraftHandler
{
    public async Task<IResult> GetMetaData(ISystemService systemService, IOptions<ServerSettings> options)
    {
        var metadataResponse = new MetadataResponse
        {
            SkinDomains = options.Value.SkinDomains,
            Meta =
            {
                ServerName = options.Value.ProjectName,
                ImplementationVersion = options.Value.ProjectVersion
            },
            SignaturePublicKey = await systemService.GetPublicKey()
        };

        return Results.Ok(metadataResponse);

    }
}