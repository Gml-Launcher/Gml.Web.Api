using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Hubs;

public class NotificationHub : BaseHub
{
    public NotificationHub(IGmlManager gmlManager)
    {
        gmlManager.Notifications.Notifications.Subscribe(notify =>
        {
            Clients.All.SendAsync("Notifications", notify);
        });
    }
}