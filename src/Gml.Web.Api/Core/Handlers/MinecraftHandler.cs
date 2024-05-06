using System.Text;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Dto.Minecraft.AuthLib;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Handlers;

public class MinecraftHandler : IMinecraftHandler
{
    public static async Task<IResult> GetMetaData(ISystemService systemService, IGmlManager gmlManager,
        IOptions<ServerSettings> options)
    {
        var skinsAddresses = options.Value.SkinDomains.ToList();
        skinsAddresses.AddRange(await GetEnvironmentAddress(gmlManager));

        skinsAddresses = skinsAddresses.Distinct().ToList();

        var metadataResponse = new MetadataResponse
        {
            SkinDomains = skinsAddresses.ToArray(),
            Meta =
            {
                ServerName = options.Value.ProjectName,
                ImplementationVersion = options.Value.ProjectVersion
            },
            SignaturePublicKey = await systemService.GetPublicKey()
        };

        return Results.Ok(metadataResponse);
    }

    private static async Task<string[]> GetEnvironmentAddress(IGmlManager gmlManager)
    {
        var domains = new List<string>();
        var skinService = await gmlManager.Integrations.GetSkinServiceAsync();
        var cloakService = await gmlManager.Integrations.GetCloakServiceAsync();

        if (!string.IsNullOrWhiteSpace(skinService))
        {
            var skinUri = new Uri(skinService);

            domains.Add(skinUri.Host);
            domains.Add($".{skinUri.Host}");
        }

        if (!string.IsNullOrWhiteSpace(cloakService))
        {
            var cloakUri = new Uri(cloakService);

            domains.Add(cloakUri.Host);
            domains.Add($".{cloakUri.Host}");
        }

        return domains.ToArray();
    }

    public static async Task<IResult> HasJoined(IGmlManager gmlManager, ISystemService systemService, string userName,
        string serverId,
        string? ip)
    {
        var user = await gmlManager.Users.GetUserByName(userName);

        if (user is null || string.IsNullOrEmpty(userName)) return Results.NoContent();

        var profile = new Profile
        {
            Id = user.Uuid,
            Name = user.Name,
            Properties = []
        };

        var texture = new PropertyTextures
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ProfileName = user.Name,
            ProfileId = user.Uuid,
            Textures = new Textures
            {
                Skin = new SkinCape
                {
                    Url = (await gmlManager.Integrations.GetSkinServiceAsync()).Replace("{userName}",
                            user.Name) + $"/skin-{user.Uuid}"
                },
                Cape = new SkinCape
                {
                    Url = (await gmlManager.Integrations.GetCloakServiceAsync()).Replace("{userName}",
                            user.Name) + $"/cape-{user.Uuid}"
                }
            }
        };

        var jsonData = JsonConvert.SerializeObject(texture);

        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
        var signature = await systemService.GetSignature(base64Value);

        profile.Properties.Add(new ProfileProperties
        {
            Value = base64Value,
            Signature = signature
        });

        return Results.Ok(profile);
    }

    public static async Task<IResult> Join(IGmlManager gmlManager, JoinRequest joinDto)
    {
        bool validateUser = await gmlManager.Users.ValidateUser(joinDto.SelectedProfile, joinDto.AccessToken);

        if (validateUser is false)
        {
            return Results.Unauthorized();
        }

        return Results.NoContent();
    }

    public static async Task<IResult> GetProfile(IGmlManager gmlManager, ISystemService systemService, string uuid,
        bool unsigned = false)
    {
        var guid = Guid.Parse(uuid);

        var guidUuid = guid.ToString().ToUpper();

        var user = await gmlManager.Users.GetUserByUuid(guidUuid);

        if (user is null || string.IsNullOrEmpty(guidUuid)) return Results.NoContent();

        var profile = new Profile
        {
            Id = uuid,
            Name = user.Name,
            Properties = []
        };

        var texture = new PropertyTextures
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ProfileName = user.Name,
            ProfileId = uuid,
            SignatureRequired = unsigned == false,
            Textures = new Textures
            {
                Skin = new SkinCape
                {
                    Url = (await gmlManager.Integrations.GetSkinServiceAsync()).Replace("{userName}",
                            user.Name) + $"/skin-{uuid}"
                },
                Cape = new SkinCape
                {
                    Url = (await gmlManager.Integrations.GetCloakServiceAsync()).Replace("{userName}",
                            user.Name) + $"/cape-{uuid}"
                }
            }
        };

        var jsonData = JsonConvert.SerializeObject(texture);

        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
        var signature = await systemService.GetSignature(base64Value);

        profile.Properties.Add(new ProfileProperties
        {
            Value = base64Value,
            Signature = signature
        });

        return Results.Ok(profile);
    }

    public static Task<IResult> GetPlayersUuids(ISystemService systemService)
    {
        return Task.FromResult(Results.Ok());
    }

    public static Task<IResult> GetPlayerAttribute(ISystemService systemService)
    {
        return Task.FromResult(Results.Ok());
    }
}
