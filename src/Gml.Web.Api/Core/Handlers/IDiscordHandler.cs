using AutoMapper;
using FluentValidation;
using Gml.Web.Api.Dto.Integration;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public interface IDiscordHandler
{
    static abstract Task<IResult> GetInfo(IGmlManager gmlManager, IMapper mapper);
    static abstract Task<IResult> UpdateInfo(IGmlManager gmlManager, IMapper mapper, IValidator<DiscordRpcUpdateDto> validator, DiscordRpcUpdateDto discordRpcUpdateDto);
}
