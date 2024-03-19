using System.ComponentModel;
using Gml.Core.Launcher;
using Gml.Core.User;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Gml.Web.Api.Core.Hubs;

public class ProfileHub : BaseHub
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
        try
        {
            if (string.IsNullOrEmpty(clientName))
                return;

            if (!_gmlManager.Profiles.CanUpdateAndRestore)
            {
                SendCallerMessage(
                    $"В данный момент происходит загрузка другого профиля, восстановление и компиляция профилей недоступна");
                return;
            }

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
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
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
            SendCallerMessage(exception.Message);
            Console.WriteLine(exception);
        }
    }

    public async Task Restore(string profileName)
    {
        try
        {
            var profile = await _gmlManager.Profiles.GetProfile(profileName);

            if (profile == null)
            {
                SendCallerMessage($"Профиль \"{profileName}\" не найден");
                return;
            }

            if (!_gmlManager.Profiles.CanUpdateAndRestore)
            {
                SendCallerMessage(
                    $"В данный момент происходит загрузка другого профиля, восстановление и компиляция профилей недоступна");
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

            SendProgress(string.Empty, new ProgressChangedEventArgs(100, 0));
            await Clients.All.SendAsync("SuccessInstalled");

            lastProgressSended = -1;
        }
        catch (Exception exception)
        {
            SendCallerMessage($"Не удалось восстановить профиль. {exception.Message}");
            Console.WriteLine(exception);
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
