using System.Collections.Concurrent;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class NotificationHub : BaseHub
{
    private ConcurrentDictionary<string, ISingleClientProxy> _connections = new();
    private static IDisposable? _event;
    
    public NotificationHub(IGmlManager gmlManager)
    {
        _event ??= gmlManager.Notifications.Notifications.Subscribe(notify =>
        {
            try
            {
                foreach (var connection in _connections.Values)
                {
                    connection.SendAsync("Notifications", notify);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    public override Task OnConnectedAsync()
    {
        _connections.TryAdd(Context.ConnectionId, Clients.Caller);
        return base.OnConnectedAsync();
    }
    
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connections.TryRemove(Context.ConnectionId, out _);
        return base.OnDisconnectedAsync(exception);
    }
}