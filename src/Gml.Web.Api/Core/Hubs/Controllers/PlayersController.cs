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
    public ConcurrentDictionary<string, IDisposable> Timers = new();
    public ConcurrentDictionary<string, ISingleClientProxy> GameServersConnections = new();
    public ConcurrentDictionary<string, UserLauncherInfo> LauncherInfos = new();

    public PlayersController(IGmlManager gmlManager)
    {
        _gmlManager = gmlManager;
    }

    public async Task AddLauncherConnection(string connectionId, ISingleClientProxy connection,
        ClaimsPrincipal contextUser)
    {
        var userName = contextUser.FindFirstValue(JwtRegisteredClaimNames.Name);
        if (!string.IsNullOrEmpty(userName) && await _gmlManager.Users.GetUserByName(userName) is AuthUser user)
        {
            LauncherInfos.TryAdd(connectionId, new UserLauncherInfo
            {
                User = user,
                ExpiredDate = DateTimeOffset.Now.AddSeconds(30),
                Connection = connection
            });

            await connection.SendAsync("RequestLauncherHash");

            var timer = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(5))
                .Subscribe(_ =>
                {
                    if (!LauncherInfos.TryGetValue(connectionId, out var userInfo)
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
        if (LauncherInfos.TryRemove(connectionId, out var user))
        {
            // Останавливаем таймер для данного лаунчера
            if (Timers.TryRemove(connectionId, out var timer))
            {
                timer.Dispose();
                Debug.WriteLine($"{user.User.Name} | {user.User.Uuid} | Timer disposed");
            }
            Debug.WriteLine($"{user.User.Name} | {user.User.Uuid} | Disconnected");

            _ = OnKickUser(user.User.Name, "Потеряно соединение с сервером");
        }
    }

    public void ConfirmLauncherHash(string connectionId, string hash)
    {
        if (LauncherInfos.TryGetValue(connectionId, out var user))
        {
            Debug.WriteLine($"{user.User.Name} | {user.User.Uuid} | Session active | hash: {hash}");
            user.ExpiredDate = DateTimeOffset.Now.AddSeconds(30);
        }
    }

    public bool GetLauncherConnection(string userName, out UserLauncherInfo? launcherInfo)
    {
        var userConnection =
            LauncherInfos.FirstOrDefault(c => c.Value.User.Name == userName);

        if (userConnection.Key is null)
        {
            launcherInfo = null;
            return false;
        }

        return LauncherInfos.TryGetValue(userConnection.Key, out launcherInfo);
    }

    internal async Task OnKickUser(string userName, string message)
    {
        foreach (var caller in GameServersConnections.Values)
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
}
