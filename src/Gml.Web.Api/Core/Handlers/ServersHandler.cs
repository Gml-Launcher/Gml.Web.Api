using System.Net;
using System.Text;
using AutoMapper;
using FluentValidation;
using Gml.Models.Servers;
using Gml.Web.Api.Domains.Servers;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Servers;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

internal abstract class ServersHandler
{
    public static async Task<IResult> GetServers(IGmlManager gmlManager, IMapper mapper, string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
            return Results.BadRequest(ResponseMessage.Create("Передан пустой параметр в качестве наименования профиля",
                HttpStatusCode.BadRequest));
        
        var profile = await gmlManager.Profiles.GetProfile(profileName);
        
        if (profile is null)
            return Results.BadRequest(ResponseMessage.Create("Профиль с данным именем не существует",
                HttpStatusCode.BadRequest));

        return Results.Ok(ResponseMessage.Create(mapper.Map<List<ServerReadDto>>(profile.Servers), string.Empty, HttpStatusCode.OK));
    }
    
    public static async Task<IResult> RemoveServer(IGmlManager gmlManager, string profileName, string serverNamesString)
    {
        if (string.IsNullOrEmpty(profileName))
            return Results.BadRequest(ResponseMessage.Create("Передан пустой параметр в качестве наименования профиля",
                HttpStatusCode.BadRequest));
        
        if (string.IsNullOrEmpty(serverNamesString))
            return Results.BadRequest(ResponseMessage.Create("Передан пустой параметр в качестве наименования сервера",
                HttpStatusCode.BadRequest));

        var serverNames = serverNamesString.Split(',');
        
        var profile = await gmlManager.Profiles.GetProfile(profileName);
        
        if (profile is null)
            return Results.BadRequest(ResponseMessage.Create("Профиль с данным именем не существует",
                HttpStatusCode.BadRequest));

        int amount = 0;
        foreach (var serverName in serverNames)
        {
            var server = profile.Servers.FirstOrDefault(c => c.Name == serverName);

            if (server is null)
            {
                continue;
            }
        
            profile.RemoveServer(server);
        
            await gmlManager.Profiles.SaveProfiles();
            amount++;
        }
        
        return Results.Ok(ResponseMessage.Create($"Обработка завершена, всего удалено: {amount}", HttpStatusCode.OK));
    }
    
    public static async Task<IResult> CreateServer(
        IGmlManager gmlManager, 
        IValidator<CreateServerDto> validator, 
        IMapper mapper,
        string profileName, 
        CreateServerDto createDto)
    {
        try
        {
            if (string.IsNullOrEmpty(profileName))
                return Results.BadRequest(ResponseMessage.Create("Передан пустой параметр в качестве наименования профиля",
                    HttpStatusCode.BadRequest));
        
            var result = await validator.ValidateAsync(createDto);

            if (!result.IsValid)
                return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                    HttpStatusCode.BadRequest));
        
            var profile = await gmlManager.Profiles.GetProfile(profileName);
        
            if (profile is null)
                return Results.BadRequest(ResponseMessage.Create("Профиль с данным именем не существует",
                    HttpStatusCode.BadRequest));
        
            var mappedServer = mapper.Map<MinecraftServer>(createDto);
            mappedServer.ServerProcedures = gmlManager.Servers;
            
            profile.AddServer(mappedServer);
            
            await gmlManager.Profiles.SaveProfiles();

            var resultObject = ResponseMessage.Create(mapper.Map<ServerReadDto>(mappedServer), "Сервер успешно добавлен",
                HttpStatusCode.Created);
            
            return Results.Created($"/api/v1/servers/{profileName}/{mappedServer.Name}", resultObject);
        }
        catch (Exception exception)
        {
            return Results.BadRequest(ResponseMessage.Create(exception.Message, HttpStatusCode.BadRequest));
        }
    }
}