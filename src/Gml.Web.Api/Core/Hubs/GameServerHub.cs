using System.Collections.Concurrent;
using System.Diagnostics;
using Gml.Web.Api.Core.Hubs.Controllers;
using Gml.Web.Api.Domains.User;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class GameServerHub(
    IGmlManager gmlManager,
    PlayersController playerController,
    HubEvents hubEvents)
    : BaseHub
{
    public override Task OnConnectedAsync()
    {
        playerController.GameServersConnections.TryAdd(Context.ConnectionId, Clients.Caller);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        playerController.GameServersConnections.TryRemove(Context.ConnectionId, out _);

        return base.OnDisconnectedAsync(exception);
    }



    public async Task OnJoin(string userName)
    {
        try
        {
            if (!playerController.GetLauncherConnection(userName, out var launcherInfo) || launcherInfo!.ExpiredDate < DateTimeOffset.Now)
            {
                hubEvents.KickUser.OnNext((userName, "Не удалось идентифицировать пользователя. Перезапустите игру вместе с лаунчером!"));
                return;
            }

            Debug.WriteLine($"OnJoin: {userName}; ExpiredTime: {launcherInfo.ExpiredDate - DateTimeOffset.Now:g}");
            var user = await gmlManager.Users.GetUserByName(userName);

            if (user is null)
            {
                await Clients.Caller.SendAsync("BanUser", userName);
                return;
            }

            await gmlManager.Users.StartSession(user);
        }
        catch (Exception e)
        {
            hubEvents.KickUser.OnNext((userName, "Произошла ошибка при попытке подключения к серверу"));
            Console.WriteLine(e);
        }
    }

    public async Task OnLeft(string userName)
    {
        try
        {
            if (!playerController.GetLauncherConnection(userName, out var launcherInfo) || launcherInfo!.ExpiredDate < DateTimeOffset.Now)
            {
                return;
            }

            var user = await gmlManager.Users.GetUserByName(userName);

            if (user is null)
            {
                await Clients.Caller.SendAsync("BanUser", userName);
                return;
            }

            Debug.WriteLine($"OnLeft: {userName}");
            await gmlManager.Users.EndSession(user);
        }
        catch (Exception e)
        {
            hubEvents.KickUser.OnNext((userName, "Произошла ошибка при попытке подключения к серверу"));
            Console.WriteLine(e);
        }

    }
}
