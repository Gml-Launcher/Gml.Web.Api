using System.Diagnostics;
using System.Reactive.Disposables;
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

    public async Task AddUserLauncher(string userName)
    {
        Debug.WriteLine($"Launcher connected: {userName}");

        _onlineUsers.AddOrUpdate(userName, new UserLauncherInfo(), (_, _)
            => new UserLauncherInfo
            {
                ExpiredDate = DateTimeOffset.Now + TimeSpan.FromSeconds(30)
            });

        await Clients.Caller.SendAsync("RequestLauncherHash");

        StartChecking(userName);
    }

    public Task UpdateUserLauncher(string userName)
    {
        Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Update launcher: {userName}");

        if (_onlineUsers.TryGetValue(userName, out var userInfo))
        {
            userInfo.ExpiredDate = DateTimeOffset.Now + TimeSpan.FromMinutes(1);
        }

        return Task.CompletedTask;
    }

    public async Task ConfirmLauncherHash(string userName)
    {
        Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Dispose timer: {userName}");

        if (_onlineUsers.Timers.TryRemove(userName, out var timer))
        {
            timer?.Dispose();
        }

        await UpdateUserLauncher(userName);
    }

    public Task RemoveUserLauncher(string userName)
    {
        Debug.WriteLine($"Launcher disconnected: {userName}");

        StopChecking(userName);

        KickUser(userName);

        return Task.CompletedTask;
    }

    public void StartChecking(string userName)
    {
        var caller = Clients.Caller;
        var scheduler = Observable.Interval(TimeSpan.FromSeconds(20));

        async void CreateScheduler(long next)
        {
            await caller.SendAsync("RequestLauncherHash");
            StartLauncherTimer(userName);
        }

        _onlineUsers.Schedulers.AddOrUpdate(
            userName,
            scheduler.Subscribe(CreateScheduler),
            (_, oldValue) =>
            {
                oldValue?.Dispose();
                return scheduler.Subscribe(CreateScheduler);
            });
    }

    private void StartLauncherTimer(string userName)
    {
        Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Start timer: {userName}");

        var timer = Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(next =>
        {
            StopChecking(userName);

            KickUser(userName);
        });

        _onlineUsers.Timers.TryAdd(userName, timer);
    }

    private void KickUser(string userName)
    {
        Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Kick from timer: {userName}");
        _hubEvents.KickUser.OnNext(userName);
    }

    public void StopChecking(string userName)
    {
        _onlineUsers.TryRemove(userName, out _);
        _onlineUsers.Schedulers.TryRemove(userName, out var scheduler);
        _onlineUsers.Timers.TryRemove(userName, out var timer);

        timer?.Dispose();
        scheduler?.Dispose();
    }
}
