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
    private ConcurrentDictionary<ISingleClientProxy, byte> _serverCaller = new();

    public GameServerHub(
        IGmlManager gmlManager,
        PlayersController onlineUsers,
        HubEvents hubEvents)
    {
        _gmlManager = gmlManager;
        _hubEvents = hubEvents;
        _onlineUsers = onlineUsers;

        hubEvents.KickUser.Subscribe(async userName => await KickUser(userName));
    }

    private async Task KickUser(string userName)
    {

        foreach (var caller in _serverCaller.Keys)
        {
            try
            {
                await caller.SendAsync("KickUser", userName, "Не удалось идентифицировать пользователя. Перезапустите лаунчер!");
                Debug.WriteLine($"User Kicked: {userName}");
            }
            catch (Exception e)
            {
                _serverCaller.TryRemove(caller, out _);
                Debug.WriteLine($"Ошибка при отправке сообщения на удаление: {e}");
            }
        }

    }

    public async Task OnJoin(string userName)
    {
        if (!_serverCaller.ContainsKey(Clients.Caller))
            _serverCaller.TryAdd(Clients.Caller, byte.MinValue);

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
