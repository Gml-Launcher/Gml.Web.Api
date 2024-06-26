using System.Collections.Concurrent;
using System.Diagnostics;
using Gml.Web.Api.Core.Hubs.Controllers;
using Gml.Web.Api.Domains.User;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class GameServerHub : BaseHub
{
    private readonly IGmlManager _gmlManager;
    private readonly PlayersController _playerController;
    private readonly HubEvents _hubEvents;

    public GameServerHub(
        IGmlManager gmlManager,
        PlayersController playerController,
        HubEvents hubEvents)
    {
        _gmlManager = gmlManager;
        _hubEvents = hubEvents;
        _playerController = playerController;

        hubEvents.KickUser.Subscribe(async userName => await KickUser(userName));
    }

    public override Task OnConnectedAsync()
    {
        _playerController.Servers.TryAdd(Context.ConnectionId, Clients.Caller);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _playerController.Servers.TryRemove(Context.ConnectionId, out _);

        return base.OnDisconnectedAsync(exception);
    }

    private async Task KickUser(string userName)
    {
        await KickUser(userName, "Не удалось идентифицировать пользователя. Перезапустите игру вместе с лаунчером!");
    }

    public async Task OnJoin(string userName)
    {
        try
        {
            if (!_playerController.GetLauncherConnection(userName, out var launcherInfo) || launcherInfo!.ExpiredDate < DateTimeOffset.Now)
            {
                await KickUser(userName);
                return;
            }

            Debug.WriteLine($"OnJoin: {userName}; ExpiredTime: {launcherInfo.ExpiredDate - DateTimeOffset.Now:g}");
            var user = await _gmlManager.Users.GetUserByName(userName);

            if (user is null)
            {
                await Clients.Caller.SendAsync("BanUser", userName);
                return;
            }

            await _gmlManager.Users.StartSession(user);
        }
        catch (Exception e)
        {
            await KickUser(userName, "Произошла ошибка при попытке подключения к серверу");
            Console.WriteLine(e);
        }
    }

    private async Task KickUser(string userName, string message)
    {
        foreach (var caller in _playerController.Servers.Values)
        {
            try
            {
                await caller.SendAsync("KickUser", userName, message);
                Debug.WriteLine($"User Kicked: {userName}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Ошибка при отправке сообщения на удаление: {e}");
            }
        }
    }

    public async Task OnLeft(string userName)
    {
        try
        {
            if (!_playerController.GetLauncherConnection(userName, out var launcherInfo) || launcherInfo!.ExpiredDate < DateTimeOffset.Now)
            {
                return;
            }

            var user = await _gmlManager.Users.GetUserByName(userName);

            if (user is null)
            {
                await Clients.Caller.SendAsync("BanUser", userName);
                return;
            }

            Debug.WriteLine($"OnLeft: {userName}");
            await _gmlManager.Users.EndSession(user);
        }
        catch (Exception e)
        {
            await KickUser(userName, "Произошла ошибка при попытке подключения к серверу");
            Console.WriteLine(e);
        }

    }
}
