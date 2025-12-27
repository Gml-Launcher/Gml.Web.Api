using System.Diagnostics;
using System.Text;
using Gml.Dto.Minecraft.AuthLib;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Handlers;

public class MinecraftHandler : IMinecraftHandler
{
    public static async Task<IResult> GetMetaData(HttpContext context, ISystemService systemService,
        IGmlManager gmlManager,
        IOptions<ServerSettings> options)
    {
        var skinsAddresses = options.Value.SkinDomains.ToList();
        var hostValue = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? context.Request.Host.Value;
        var address = new Uri($"{context.Request.Scheme}://{hostValue}");

        skinsAddresses.AddRange(await GetEnvironmentAddress(gmlManager));
        skinsAddresses.Add($"{address.Host}");
        skinsAddresses.Add($".{address.Host}");

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

    public static async Task<IResult> HasJoined(HttpContext context, IGmlManager gmlManager,
        ISystemService systemService, string userName,
        string serverId,
        string? ip)
    {
        var user = await gmlManager.Users.GetUserByName(userName);

        if (user is null || string.IsNullOrEmpty(userName) || user.IsBanned ||
            !await gmlManager.Users.CanJoinToServer(user, serverId))
            return Results.NoContent();

        var profile = new Profile
        {
            Id = user.Uuid,
            Name = user.Name,
            Properties = []
        };

        var textureProtocol = EnumExtensions.GetDisplayName(gmlManager.LauncherInfo.StorageSettings.TextureProtocol)
                                  ?.ToLower()
                              ?? EnumExtensions.GetDisplayName(TextureProtocol.Https).ToLower();

        var hostValue = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? context.Request.Host.Value;
        var address = $"{textureProtocol}://{hostValue}";

        var texture = new PropertyTextures
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ProfileName = user.Name,
            ProfileId = user.Uuid,
            Textures = new Textures
            {
                Skin = !string.IsNullOrEmpty(user.TextureSkinGuid)
                    ? new SkinCape
                    {
                        Url = string.Concat(address, $"/api/v1/integrations/texture/skins/{user.TextureSkinGuid}"),
                        Metadata = user.IsSlim ? new SkinMetadata { Model = "slim" } : null
                    }
                    : null,
                Cape = !string.IsNullOrEmpty(user.TextureCloakGuid)
                    ? new SkinCape
                    {
                        Url = string.Concat(address, $"/api/v1/integrations/texture/capes/{user.TextureCloakGuid}")
                    }
                    : null
            }
        };

        if (!string.IsNullOrEmpty(user.TextureSkinGuid))
        {
            texture.Textures.Skin = new SkinCape
            {
                Url = string.Concat(address, $"/api/v1/integrations/texture/skins/{user.TextureSkinGuid}"),
                Metadata = user.IsSlim ? new SkinMetadata { Model = "slim" } : null
            };
        }

        if (!string.IsNullOrEmpty(user.TextureCloakGuid))
        {
            texture.Textures.Cape = new SkinCape
            {
                Url = string.Concat(address, $"/api/v1/integrations/texture/capes/{user.TextureCloakGuid}")
            };
        }

        Debug.WriteLine(string.Join(Environment.NewLine, texture.Textures.Skin?.Url, texture.Textures.Cape?.Url));

        var jsonData = JsonConvert.SerializeObject(texture);

        Debug.WriteLine($"New user {jsonData}");

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
        bool validateUser =
            await gmlManager.Users.ValidateUser(joinDto.SelectedProfile, joinDto.ServerId, joinDto.AccessToken);

        if (!validateUser)
        {
            return Results.Unauthorized();
        }

        return Results.NoContent();
    }

    public static async Task<IResult> GetProfile(HttpContext context, IGmlManager gmlManager,
        ISystemService systemService, string uuid,
        bool unsigned = false)
    {
        var guid = Guid.Parse(uuid);

        var guidUuid = guid.ToString().ToUpper();

        var user = await gmlManager.Users.GetUserByUuid(guidUuid);

        if (user is null || string.IsNullOrEmpty(guidUuid) || user.IsBanned) return Results.NoContent();

        var profile = new Profile
        {
            Id = uuid,
            Name = user.Name,
            Properties = []
        };

        var textureProtocol = EnumExtensions.GetDisplayName(gmlManager.LauncherInfo.StorageSettings.TextureProtocol)
                                  ?.ToLower()
                              ?? EnumExtensions.GetDisplayName(TextureProtocol.Https).ToLower();

        var hostValue = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? context.Request.Host.Value;
        var address = $"{textureProtocol}://{hostValue}";

        var texture = new PropertyTextures
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ProfileName = user.Name,
            ProfileId = uuid,
            SignatureRequired = !unsigned,
            Textures = new Textures
            {
                Skin = !string.IsNullOrEmpty(user.TextureSkinGuid)
                    ? new SkinCape
                    {
                        Url = string.Concat(address, $"/api/v1/integrations/texture/skins/{user.TextureSkinGuid}"),
                        Metadata = user.IsSlim ? new SkinMetadata { Model = "slim" } : null
                    }
                    : null,
                Cape = !string.IsNullOrEmpty(user.TextureCloakGuid)
                    ? new SkinCape
                    {
                        Url = string.Concat(address, $"/api/v1/integrations/texture/capes/{user.TextureCloakGuid}")
                    }
                    : null
            }
        };

        //Debug.WriteLine(string.Join(Environment.NewLine, texture.Textures.Skin.Url, texture.Textures.Cape?.Url));

        var jsonData = JsonConvert.SerializeObject(texture);

        Debug.WriteLine($"New user {jsonData}");

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

    public static async Task<IResult> GetUserUuidByName(HttpContext context, IGmlManager gmlManager, string name)
    {
        var user = await gmlManager.Users.GetUserByName(name);

        if (user is null)
        {
            return Results.Ok(new
            {
                name = name,
                id = Guid.Empty.ToString().ToLower()
            });
        }

        return Results.Ok(new
        {
            name = user.Name,
            id = user.Uuid
        });
    }

    public static async Task<IResult> GetUsersUuidByNames(HttpContext context, IGmlManager gmlManager,
        [FromBody] string[] names)
    {
        List<object> items = new();

        foreach (var name in names)
        {
            var user = await gmlManager.Users.GetUserByName(name);

            if (user is not null)
            {
                items.Add(new
                {
                    name = user.Name,
                    id = user.Uuid
                });
            }
        }

        return Results.Ok(items);
    }
}
