using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class BaseHub : Hub
{
    protected async void ChangeProgress(string prefix, int percents)
    {
        try
        {
            await Clients.All.SendAsync($"{prefix}ChangeProgress", percents);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    protected async void SendCallerMessage(string content)
    {
        try
        {
            await Clients.Caller.SendAsync("Message", content);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    protected async void Log(string message, string profileName)
    {
        try
        {
            await Clients.All.SendAsync("Log", profileName, message);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    protected async void Log(string message)
    {
        try
        {
            await Clients.All.SendAsync("Log", message);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}
