using System.Collections.Concurrent;
using Gml.Web.Api.Core.Hubs.Controllers;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class NotificationHub : BaseHub
{
    private readonly NotificationController _notificationController;
    private static IDisposable? _event;

    public NotificationHub(IGmlManager gmlManager, NotificationController notificationController)
    {
        _notificationController = notificationController;
        _event ??= gmlManager.Notifications.Notifications.Subscribe(notify =>
        {
            foreach (var connection in _notificationController.Connections)
            {
                try
                {
                    connection.SendAsync("Notifications", notify);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        });
    }

    public override Task OnConnectedAsync()
    {
        _notificationController.Add(Context.ConnectionId, Clients.Caller);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _notificationController.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
