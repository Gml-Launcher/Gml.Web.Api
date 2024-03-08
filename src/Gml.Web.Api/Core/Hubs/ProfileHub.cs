using System.ComponentModel;
using Gml.Core.Launcher;
using Gml.Core.User;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class ProfileHub : Hub
{
    private readonly IGmlManager _gmlManager;
    private int lastPackProgressSended = -1;

    private int lastProgressSended = -1;

    public ProfileHub(IGmlManager gmlManager)
    {
        _gmlManager = gmlManager;
    }

    public async Task Build(string clientName)
    {
        if (string.IsNullOrEmpty(clientName))
            return;

        var profile = await _gmlManager.Profiles.GetProfile(clientName);

        if (profile is null)
            return;

        await Clients.All.SendAsync("FileChanged", "Packaging...");

        _gmlManager.Profiles.PackChanged += ChangePackProgress;
        await _gmlManager.Profiles.PackProfile(profile);
        _gmlManager.Profiles.PackChanged -= ChangePackProgress;

        await Clients.All.SendAsync("SuccessPacked");
        lastPackProgressSended = -1;
    }

    private async void ChangePackProgress(ProgressChangedEventArgs e)
    {
        try
        {
            if (lastPackProgressSended == e.ProgressPercentage) return;

            lastProgressSended = e.ProgressPercentage;

            await Clients.All.SendAsync("ChangeProgress", e?.ProgressPercentage);
            await Clients.Others.SendAsync("BlockRestore");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    public async Task Restore(string profileName)
    {
        var profile = await _gmlManager.Profiles.GetProfile(profileName);

        if (profile == null)
        {
            await Clients.Caller.SendAsync("Message", $"Профиль \"{profileName}\" не найден");
            return;
        }
        SendProgress(string.Empty, new ProgressChangedEventArgs(0, 0));

        profile.GameLoader.ProgressChanged += SendProgress;

        await _gmlManager.Profiles.RestoreProfileInfo(profile.Name, new StartupOptions
        {
            OsType = OsType.Linux,
            OsArch = "64"
        }, User.Empty);

        await _gmlManager.Profiles.RestoreProfileInfo(profile.Name, new StartupOptions
        {
            OsType = OsType.OsX,
            OsArch = "64"
        }, User.Empty);

        await _gmlManager.Profiles.RestoreProfileInfo(profile.Name, new StartupOptions
        {
            OsType = OsType.Windows,
            OsArch = "64"
        }, User.Empty);

        profile.GameLoader.ProgressChanged -= SendProgress;

        try
        {
            SendProgress(string.Empty, new ProgressChangedEventArgs(100, 0));
            await Clients.All.SendAsync("SuccessInstalled");

            lastProgressSended = -1;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async void SendFileChanged(string file)
    {
        try
        {
            if (!string.IsNullOrEmpty(file))
                await Clients.Caller.SendAsync("FileChanged", file);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async void SendProgress(object sender, ProgressChangedEventArgs e)
    {
        try
        {
            if (lastProgressSended == e.ProgressPercentage) return;

            lastProgressSended = e.ProgressPercentage;
            await Clients.All.SendAsync("ChangeProgress", e?.ProgressPercentage);
            await Clients.Others.SendAsync("BlockRestore");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}
