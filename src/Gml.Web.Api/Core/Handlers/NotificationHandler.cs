using System.Net;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public class NotificationHandler : INotificationsHandler
{
    public static Task<IResult> GetNotifications(IGmlManager gmlManager)
    {
        var result = Results.Ok(ResponseMessage.Create(gmlManager.Notifications.History, "Список уведомлений",
            HttpStatusCode.OK));

        return Task.FromResult(result);
    }
}