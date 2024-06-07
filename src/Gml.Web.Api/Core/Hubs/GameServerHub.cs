using System.Collections.Concurrent;
using System.Diagnostics;
using Gml.Web.Api.Domains.User;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class GameServerHub : BaseHub
{
    private readonly IGmlManager _gmlManager;
    private readonly PlayersController _onlineUsers;
    private readonly HubEvents _hubEvents;
    private ISingleClientProxy _serverCaller;

    public GameServerHub(
        IGmlManager gmlManager,
        PlayersController onlineUsers,
        HubEvents hubEvents)
    {
        _gmlManager = gmlManager;
        _hubEvents = hubEvents;
        _onlineUsers = onlineUsers;

        _hubEvents.KickUser.Subscribe(async userName => await KickUser(userName));
    }

    private async Task KickUser(string userName)
    {
        try
        {
            await _serverCaller.SendAsync("KickUser", userName,
                "Не удалось идентифицировать пользователя. Перезапустите лаунчер!");
        }
        catch (Exception e)
        {
            // Debug.Write
        }

    }

    public async Task OnJoin(string userName)
    {
        _serverCaller = Clients.Caller;

        if (!_onlineUsers.TryGetValue(userName, out var launcherInfo) || launcherInfo.ExpiredDate < DateTimeOffset.Now)
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

    public async Task OnLeft(string userName)
    {
        var user = await _gmlManager.Users.GetUserByName(userName);

        if (user is null)
        {
            await Clients.Caller.SendAsync("BanUser", userName);
            return;
        }

        if (!_onlineUsers.TryGetValue(userName, out var launcherInfo))
        {
            Debug.WriteLine($"OnLeft: {userName}");
            await _gmlManager.Users.EndSession(user);
        }
    }
}
