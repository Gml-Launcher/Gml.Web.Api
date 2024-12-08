using System.Diagnostics;
using System.Reactive.Subjects;
using Gml.Web.Api.Core.Hubs.Controllers;

namespace Gml.Web.Api.Core.Hubs;

public class HubEvents
{
    public Subject<(string UserName, string Reason)> KickUser { get; } = new();

    public HubEvents(PlayersController playersController)
    {
        KickUser.Subscribe(async void (user) =>
        {
            try
            {
                await playersController.OnKickUser(user.UserName, user.Reason);
            }
            catch (Exception e)
            {
                //ToDo Add Sentry Log
            }
        });
    }
}
