using System.Reactive.Subjects;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Settings;
using GmlCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Repositories;

public class SettingsRepository(
    DatabaseContext databaseContext,
    ServerSettings options,
    IGmlManager gmlManager,
    ISubject<Settings> settingsObservable)
    : ISettingsRepository
{
    private readonly ServerSettings _options = options;
    private readonly IObserver<Settings> _settingsObservable = settingsObservable;

    public async Task<Settings?> UpdateSettings(Settings settings)
    {
        gmlManager.LauncherInfo.UpdateSettings(settings.StorageType,settings.StorageHost, settings.StorageLogin, settings.StoragePassword, settings.TextureProtocol);

        await databaseContext.AddAsync(settings);
        await databaseContext.SaveChangesAsync();

        _settingsObservable.OnNext(settings);

        return settings;
    }

    public Task<Settings?> GetSettings()
    {
        return databaseContext.Settings.OrderByDescending(c => c.Id).FirstOrDefaultAsync();
    }
}
