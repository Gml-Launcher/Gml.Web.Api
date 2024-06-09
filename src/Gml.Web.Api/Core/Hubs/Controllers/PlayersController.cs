using System.Collections.Concurrent;
using System.Diagnostics;
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
    public ConcurrentDictionary<string, IDisposable> Schedulers = new();
    public ConcurrentDictionary<string, ISingleClientProxy> Servers = new();
    public ConcurrentDictionary<string, UserLauncherInfo> LauncherConnections = new();

    public PlayersController(IGmlManager gmlManager)
    {
        _gmlManager = gmlManager;
    }

    public void AddPlayer(string connectionId, ISingleClientProxy connection, string userName)
    {
        // Launchers.TryAdd()
        // _playerController.AddOrUpdate(userName, new UserLauncherInfo(), (_, _)
        //     => new UserLauncherInfo
        //     {
        //         ExpiredDate = DateTimeOffset.Now + TimeSpan.FromSeconds(30)
        //     });
        //
        // await Clients.Caller.SendAsync("RequestLauncherHash");
        //
        // StartChecking(userName);
    }

    public async Task AddLauncherConnection(string connectionId, ISingleClientProxy connection, ClaimsPrincipal contextUser)
    {
        var userName = contextUser.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value;

        if (!string.IsNullOrEmpty(userName) && await _gmlManager.Users.GetUserByName(userName) is AuthUser user)
        {
            LauncherConnections.TryAdd(connectionId, new UserLauncherInfo
            {
                User = user,
                ExpiredDate = DateTimeOffset.Now.AddSeconds(30)
            });

            Debug.WriteLine("New launcher connected");

        }

    }

    public void RemoveLauncherConnection(string connectionId)
    {
        LauncherConnections.TryRemove(connectionId, out _);
    }
}
