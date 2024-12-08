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
}
