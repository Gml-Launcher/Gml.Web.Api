using System.ComponentModel;
using Gml.Core.Launcher;
using Gml.Core.User;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Hubs;

public class ProfileHub : BaseHub
{
    private readonly IGmlManager _gmlManager;
    private double lastPackProgress = -1;

    private double lastProgress = -1;

    public ProfileHub(IGmlManager gmlManager)
    {
        _gmlManager = gmlManager;
    }

    public async Task Build(string profileName)
    {
        try
        {
            if (string.IsNullOrEmpty(profileName))
                return;

            var profile = await _gmlManager.Profiles.GetProfile(profileName);

            if (profile is null)
                return;

            if (profile.State is ProfileState.Loading or ProfileState.Packing)
            {
                SendCallerMessage(
                    "В данный момент уже происходит загрузка выбранного профиля!");
                return;
            }

            Log("Preparation for packaging...", profileName);

            var eventInfo = _gmlManager.Profiles.PackChanged.Subscribe(percentage =>
            {
                ChangePackProgress(profileName, percentage);
            });
            await _gmlManager.Profiles.PackProfile(profile);
            await Clients.All.SendAsync("SuccessPacked", profileName);
            eventInfo.Dispose();
            lastPackProgress = -1;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    private async void ChangePackProgress(string profileName, double percentage)
    {
        try
        {
            if (Math.Abs(lastPackProgress - percentage) < 0.001) return;

            lastPackProgress = percentage;

            await Clients.All.SendAsync("ChangeProgress", profileName, percentage);
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

            if (profile.State == ProfileState.Loading)
            {
                SendCallerMessage(
                    "В данный момент уже происходит загрузка выбранного профиля!");
                return;
            }

            var fullPercentage = profile.GameLoader.FullPercentages.Subscribe(percentage =>
            {
                SendProgress("FullProgress", profile.Name, percentage);
            });

            var loadPercentage = profile.GameLoader.LoadPercentages.Subscribe(percentage =>
            {
                SendProgress("ChangeProgress", profile.Name, percentage);
            });

            var logInfo = profile.GameLoader.LoadLog.Subscribe(logs =>
            {
                Log(logs, profile.Name);
            });

            var exception = profile.GameLoader.LoadException.Subscribe(async logs =>
            {
                try
                {
                    await Clients.All.SendAsync("OnException", profile.Name, logs.ToString());
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    _gmlManager.BugTracker.CaptureException(exception);
                }
            });

            await _gmlManager.Profiles.RestoreProfileInfo(profile.Name);

            await Clients.All.SendAsync("SuccessInstalled", profile.Name);

            fullPercentage.Dispose();
            loadPercentage.Dispose();
            logInfo.Dispose();
            exception.Dispose();

            lastProgress = -1;
        }
        catch (Exception exception)
        {
            _gmlManager.BugTracker.CaptureException(exception);
            SendCallerMessage($"Не удалось восстановить профиль. {exception.Message}");
            Console.WriteLine(exception);
        }
    }

    private async void SendProgress(string name, string profileName, double percentage)
    {
        try
        {
            if (Math.Abs(lastProgress - percentage) < 0.000) return;

            var percentageValue = Math.Round(percentage, 2);

            if (double.IsPositiveInfinity(percentageValue) || double.IsNegativeInfinity(percentageValue))
            {
                return;
            }

            if (double.IsNaN(percentageValue) || double.IsNaN(percentageValue))
            {
                return;
            }

            lastProgress = percentage;
            await Clients.All.SendAsync(name, profileName, percentageValue);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    public class ConfigureJsonOptions : IConfigureOptions<JsonOptions>
    {
        public void Configure(JsonOptions options)
        {
            options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
        }
    }
}
