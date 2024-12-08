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
    private static IDisposable? _profilesChangedEvent;

    public LauncherHub(
        IGmlManager gmlManager,
        PlayersController playerController,
        HubEvents hubEvents)
    {
        _gmlManager = gmlManager;
        _hubEvents = hubEvents;
        _playerController = playerController;

        _profilesChangedEvent ??= gmlManager.Profiles.ProfilesChanged.Subscribe(eventType =>
        {
            foreach (var connection in _playerController.LauncherInfos.Values.Select(c => c.Connection).OfType<ISingleClientProxy>())
            {
                connection?.SendAsync("RefreshProfiles");
            }
        });
    }

    public void ConfirmLauncherHash(string hash)
    {
        _playerController.ConfirmLauncherHash(Context.ConnectionId, hash);
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
}
