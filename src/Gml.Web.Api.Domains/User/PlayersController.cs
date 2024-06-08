using System;
using System.Collections.Concurrent;

namespace Gml.Web.Api.Domains.User;

public class PlayersController : ConcurrentDictionary<string, UserLauncherInfo>
{
    public ConcurrentDictionary<string, IDisposable> Timers = new();
    public ConcurrentDictionary<string, IDisposable> Schedulers = new();

}
