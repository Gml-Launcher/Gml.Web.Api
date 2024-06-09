using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Gml.Web.Api.Core.Hubs.Controllers;
using Gml.Web.Api.Domains.User;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class LauncherHub : BaseHub
{
    private readonly IGmlManager _gmlManager;
    private readonly HubEvents _hubEvents;
    private readonly PlayersController _playerController;

    public LauncherHub(
        IGmlManager gmlManager,
        PlayersController playerController,
        HubEvents hubEvents)
    {
        _gmlManager = gmlManager;
        _hubEvents = hubEvents;
        _playerController = playerController;
    }

    public override Task OnConnectedAsync()
    {
        if (Context.User is null)
        {
            return Task.CompletedTask;
        }

        _ = _playerController.AddLauncherConnection(Context.ConnectionId, Clients.Caller, Context.User);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _playerController.RemoveLauncherConnection(Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }

    // public async Task AddUserLauncher(string userName)
    // {
    //     Debug.WriteLine($"Launcher connected: {userName}");
    //
    //     _playerController.AddPlayer(Context.ConnectionId, Clients.Caller, userName);
    // }
    //
    // public Task UpdateUserLauncher(string userName)
    // {
    //     Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Update launcher: {userName}");
    //
    //     if (_playerController.TryGetValue(userName, out var userInfo))
    //     {
    //         userInfo.ExpiredDate = DateTimeOffset.Now + TimeSpan.FromMinutes(1);
    //     }
    //
    //     return Task.CompletedTask;
    // }
    //
    // public async Task ConfirmLauncherHash(string userName)
    // {
    //     Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Dispose timer: {userName}");
    //
    //     if (_playerController.Timers.TryRemove(userName, out var timer))
    //     {
    //         timer?.Dispose();
    //     }
    //
    //     await UpdateUserLauncher(userName);
    // }
    //
    // public Task RemoveUserLauncher(string userName)
    // {
    //     Debug.WriteLine($"Launcher disconnected: {userName}");
    //
    //     StopChecking(userName);
    //
    //     KickUser(userName);
    //
    //     return Task.CompletedTask;
    // }
    //
    // public void StartChecking(string userName)
    // {
    //     var caller = Clients.Caller;
    //     var scheduler = Observable.Interval(TimeSpan.FromSeconds(20));
    //
    //     async void CreateScheduler(long next)
    //     {
    //         await caller.SendAsync("RequestLauncherHash");
    //         StartLauncherTimer(userName);
    //     }
    //
    //     _playerController.Schedulers.AddOrUpdate(
    //         userName,
    //         scheduler.Subscribe(CreateScheduler),
    //         (_, oldValue) =>
    //         {
    //             oldValue?.Dispose();
    //             return scheduler.Subscribe(CreateScheduler);
    //         });
    // }
    //
    // private void StartLauncherTimer(string userName)
    // {
    //     Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Start timer: {userName}");
    //
    //     var timer = Observable.Timer(TimeSpan.FromSeconds(30)).Subscribe(next =>
    //     {
    //         StopChecking(userName);
    //
    //         KickUser(userName);
    //     });
    //
    //     _playerController.Timers.TryAdd(userName, timer);
    // }
    //
    // private void KickUser(string userName)
    // {
    //     Debug.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss:fff}] Kick from timer: {userName}");
    //     _hubEvents.KickUser.OnNext(userName);
    // }
    //
    // public void StopChecking(string userName)
    // {
    //     _playerController.TryRemove(userName, out _);
    //     _playerController.Schedulers.TryRemove(userName, out var scheduler);
    //     _playerController.Timers.TryRemove(userName, out var timer);
    //
    //     timer?.Dispose();
    //     scheduler?.Dispose();
    // }
}
