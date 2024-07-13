using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs.Controllers;

public class NotificationController
{
    private ConcurrentDictionary<string, ISingleClientProxy> _connections = new();

    public IEnumerable<ISingleClientProxy> Connections => _connections.Values;

    public void Add(string connection, ISingleClientProxy caller)
    {
        _connections.TryAdd(connection, caller);
    }

    public void Remove(string connection)
    {
        _connections.TryRemove(connection, out _);
    }
}
