using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using Gml.Web.Api.Domains.User;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class LauncherHub : BaseHub
{
    private readonly IGmlManager _gmlManager;
    private readonly HubEvents _hubEvents;
    private readonly PlayersController _onlineUsers;

    public LauncherHub(
        IGmlManager gmlManager,
        PlayersController onlineUsers,
        HubEvents hubEvents)
    {
        _gmlManager = gmlManager;
        _hubEvents = hubEvents;
        _onlineUsers = onlineUsers;
    }

    public Task AddUserLauncher(string userName)
    {
        Debug.WriteLine($"Launcher connected: {userName}");

        _onlineUsers.AddOrUpdate(userName, new UserLauncherInfo(), (_, _)
            => new UserLauncherInfo
            {
                ExpiredDate = DateTimeOffset.Now + TimeSpan.FromSeconds(30)
            });

        return Task.CompletedTask;
    }

    public async Task UpdateUserLauncher(string userName)
    {
        Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Update launcher: {userName}");

        if (_onlineUsers.TryGetValue(userName, out var userInfo))
        {
            userInfo.ExpiredDate = DateTimeOffset.Now + TimeSpan.FromMinutes(1);
        }

        await Clients.Caller.SendAsync("RequestLauncherHash");
        StartLauncherTimer(userName);
    }

    public void ConfirmLauncherHash(string userName)
    {
        Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Dispose timer: {userName}");
        if (_onlineUsers.Timers.TryRemove(userName, out var timer))
        {
            timer?.Dispose();
        }
    }

    private void StartLauncherTimer(string userName)
    {
        Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Start timer: {userName}");
        _onlineUsers.Timers.AddOrUpdate(userName,
            Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(next =>
            {
                if (_onlineUsers.Timers.TryRemove(userName, out var timer))
                {
                    timer?.Dispose();
                    Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Kick from timer: {userName}");
                    _hubEvents.KickUser.OnNext(userName);
                    _onlineUsers.TryRemove(userName, out _);
                }
            }),
            (key, oldValue) =>
            {
                oldValue?.Dispose();

                return Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(next =>
                {
                    if (_onlineUsers.Timers.TryRemove(userName, out var timer))
                    {
                        timer?.Dispose();
                        Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Kick from timer: {userName}");
                        _hubEvents.KickUser.OnNext(userName);
                        _onlineUsers.TryRemove(userName, out _);
                    }
                });
            });
    }

    public Task RemoveUserLauncher(string userName)
    {
        Debug.WriteLine($"Launcher disconnected: {userName}");
        _onlineUsers.TryRemove(userName, out _);

        return Task.CompletedTask;
    }
}
