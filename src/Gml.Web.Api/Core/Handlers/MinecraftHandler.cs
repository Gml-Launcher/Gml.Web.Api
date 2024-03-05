using System.Text;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Dto.Minecraft.AuthLib;
using GmlCore.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Handlers;

public class MinecraftHandler : IMinecraftHandler
{
    public static async Task<IResult> GetMetaData(ISystemService systemService, IOptions<ServerSettings> options)
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

    public static async Task<IResult> HasJoined(ISystemService systemService, string username, string serverId,
        string? ip)
    {
        return Results.Ok();
    }

    public static async Task<IResult> Join(JoinRequest joinDto)
    {
        return Results.StatusCode(StatusCodes.Status204NoContent);
    }

    public static async Task<IResult> GetProfile(IGmlManager gmlManager, ISystemService systemService, string uuid,
        bool unsigned = false)
    {
        var user = "GamerVII";

        var profile = new Profile()
        {
            Id = uuid,
            Name = user,
            Properties = []
        };

        var texture = new PropertyTextures()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ProfileName = user,
            ProfileId = uuid,
            SignatureRequired = unsigned == false,
            Textures = new Textures
            {
                Skin = new SkinCape()
                {
                    Url = (await gmlManager.Integrations.GetSkinServiceAsync()).Replace("{userName}", user) +
                          $"?{DateTime.Now.Millisecond}"
                },
                Cape = new SkinCape()
                {
                    Url = (await gmlManager.Integrations.GetClockServiceAsync()).Replace("{userName}", user) +
                          $"?{DateTime.Now.Millisecond}"
                }
            }
        };

        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(texture)));
        var signature = await systemService.GetSignature(base64Value);

        profile.Properties.Add(new ProfileProperties()
        {
            Value = base64Value,
            Signature = signature
        });


        return Results.Ok(profile);
    }

    public static async Task<IResult> GetPlayersUuids(ISystemService systemService)
    {
        return Results.Ok();
    }

    public static async Task<IResult> GetPlayerAttribute(ISystemService systemService)
    {
        return Results.Ok();
    }
}
