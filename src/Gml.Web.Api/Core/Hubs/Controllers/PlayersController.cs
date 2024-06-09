using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Security.Claims;
using Gml.Core.Launcher;
using Gml.Core.User;
using Gml.Web.Api.Domains.User;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Launcher;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Gml.Web.Api.Core.Hubs.Controllers;

public class PlayersController : ConcurrentDictionary<string, UserLauncherInfo>
{
    private readonly IGmlManager _gmlManager;
    private readonly HubEvents _hubEvents;
    public ConcurrentDictionary<string, IDisposable> Timers = new();
    public ConcurrentDictionary<string, IDisposable> Schedulers = new();
    public ConcurrentDictionary<string, ISingleClientProxy> Servers = new();
    public ConcurrentDictionary<string, UserLauncherInfo> LauncherConnections = new();

    public PlayersController(IGmlManager gmlManager, HubEvents hubEvents)
    {
        _hubEvents = hubEvents;
        _gmlManager = gmlManager;
    }

    public async Task AddLauncherConnection(string connectionId, ISingleClientProxy connection,
        ClaimsPrincipal contextUser)
    {
        var userName = contextUser.FindFirstValue(JwtRegisteredClaimNames.Name);
        if (!string.IsNullOrEmpty(userName) && await _gmlManager.Users.GetUserByName(userName) is AuthUser user)
        {
            LauncherConnections.TryAdd(connectionId, new UserLauncherInfo
            {
                User = user,
                ExpiredDate = DateTimeOffset.Now.AddSeconds(30),
                Connection = connection
            });

            await connection.SendAsync("RequestLauncherHash");

            var timer = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(5))
                .Subscribe(_ =>
                {
                    if (!LauncherConnections.TryGetValue(connectionId, out var userInfo)
                        || DateTimeOffset.Now > userInfo.ExpiredDate)
                    {
                        RemoveLauncherConnection(connectionId);
                    }
                    else
                    {
                        connection.SendAsync("RequestLauncherHash");
                    }
                });

            Timers.TryAdd(connectionId, timer);

            Debug.WriteLine($"{user.Name} | {user.Uuid} | Connected");
        }
    }

    public void RemoveLauncherConnection(string connectionId)
    {
        if (LauncherConnections.TryRemove(connectionId, out var user))
        {
            // Останавливаем таймер для данного лаунчера
            if (Timers.TryRemove(connectionId, out var timer))
            {
                timer.Dispose();
                Debug.WriteLine($"{user.User.Name} | {user.User.Uuid} | Timer disposed");
            }
            Debug.WriteLine($"{user.User.Name} | {user.User.Uuid} | Disconnected");

            _hubEvents.KickUser.OnNext(user.User.Name);
        }
    }

    public void ConfirmLauncherHash(string connectionId, string hash)
    {
        if (LauncherConnections.TryGetValue(connectionId, out var user))
        {
            Debug.WriteLine($"{user.User.Name} | {user.User.Uuid} | Session active");
            user.ExpiredDate = DateTimeOffset.Now.AddSeconds(30);
        }
    }

    public bool GetLauncherConnection(string userName, out UserLauncherInfo? launcherInfo)
    {
        var userConnection =
            LauncherConnections.FirstOrDefault(c => c.Value.User.Name == userName);

        return LauncherConnections.TryGetValue(userConnection.Key, out launcherInfo);
    }
}
