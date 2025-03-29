using System.Collections.Frozen;
using System.Net;
using AutoMapper;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Player;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public class PlayersHandler : IPlayersHandler
{
    public static async Task<IResult> GetPlayers(IGmlManager gmlManager, IMapper mapper, int? take, int? offset, string? findName)
    {
        var players = await gmlManager.Users.GetUsers(take ?? 20, offset ?? 0, findName ?? string.Empty);

        return Results.Ok(ResponseMessage.Create(mapper.Map<List<ExtendedPlayerReadDto>>(players), "Список пользователей успешно получен", HttpStatusCode.OK));
    }

    public static async Task<IResult> BanPlayer(
        IGmlManager gmlManager,
        IMapper mapper,
        IList<string> playerUuids,
        bool deviceBlock = false)
    {
        if (!playerUuids.Any())
        {
            return Results.BadRequest(ResponseMessage.Create("Не передан ни один пользователь для блокировки",
                HttpStatusCode.BadRequest));
        }

        foreach (var playerUuid in playerUuids)
        {
            var player = await gmlManager.Users.GetUserByUuid(playerUuid);

            if (player is null) continue;
            player.IsBanned = true;
            await gmlManager.Users.UpdateUser(player);
        }

        return Results.Ok(ResponseMessage.Create("Пользователь(и) успешно заблокированы", HttpStatusCode.OK));
    }

    public static async Task<IResult> RemovePlayer(
        IGmlManager gmlManager,
        IMapper mapper,
        IList<string> playerUuids)
    {
        if (!playerUuids.Any())
        {
            return Results.BadRequest(ResponseMessage.Create("Не передан ни один пользователь для блокировки",
                HttpStatusCode.BadRequest));
        }

        var profiles = (await gmlManager.Profiles.GetProfiles()).ToFrozenSet();



        foreach (var playerUuid in playerUuids)
        {

            var player = await gmlManager.Users.GetUserByUuid(playerUuid);

            if (player is null) continue;

            if (profiles.Any(c => c.UserWhiteListGuid.Contains(playerUuid)))
            {
                return Results.BadRequest(ResponseMessage.Create($"Пользователь \"{player.Name}\" находится в белом списке, удалите его из всех профилей перед удалением!",
                    HttpStatusCode.BadRequest));
            }

            await gmlManager.Users.RemoveUser(player);
        }

        return Results.Ok(ResponseMessage.Create("Пользователь(и) успешно заблокированы", HttpStatusCode.OK));
    }

    public static async Task<IResult> PardonPlayer(
        IGmlManager gmlManager,
        IMapper mapper,
        IList<string> playerUuids)
    {
        if (!playerUuids.Any())
        {
            return Results.BadRequest(ResponseMessage.Create("Не передан ни один пользователь для блокировки",
                HttpStatusCode.BadRequest));
        }

        foreach (var playerUuid in playerUuids)
        {
            var player = await gmlManager.Users.GetUserByUuid(playerUuid);

            if (player is null) continue;
            player.IsBanned = false;
            await gmlManager.Users.UpdateUser(player);
        }

        return Results.Ok(ResponseMessage.Create("Пользователь(и) успешно разблокированы", HttpStatusCode.OK));
    }
}
