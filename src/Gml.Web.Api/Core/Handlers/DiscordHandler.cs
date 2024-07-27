using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Web.Api.Domains.Integrations;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Gml.Web.Api.Core.Handlers;

public class DiscordHandler : IDiscordHandler
{
    public static async Task<IResult> GetInfo(IGmlManager gmlManager, IMapper mapper)
    {
        var discordRpcInfo = await gmlManager.Integrations.GetDiscordRpc();

        if (discordRpcInfo is null)
            return Results.NotFound(ResponseMessage.Create("Сервис DiscordRPC не настроен", HttpStatusCode.NotFound));

        return Results.Ok(ResponseMessage.Create(mapper.Map<DiscordRpcReadDto>(discordRpcInfo), "Сервис DiscordRPC успешно получен", HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> UpdateInfo(IGmlManager gmlManager, IMapper mapper, IValidator<DiscordRpcUpdateDto> validator, DiscordRpcUpdateDto discordRpcUpdateDto)
    {
        var result = await validator.ValidateAsync(discordRpcUpdateDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        await gmlManager.Integrations.UpdateDiscordRpc(mapper.Map<DiscordRpcClient>(discordRpcUpdateDto));

        return Results.Ok(ResponseMessage.Create("Сервис DiscordRPC успешно обновлен", HttpStatusCode.OK));
    }
}
