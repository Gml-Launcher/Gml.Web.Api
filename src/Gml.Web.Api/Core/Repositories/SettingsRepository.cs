using System.Reactive.Subjects;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOptions<ServerSettings> _options;
    private readonly IObserver<Settings> _settingsObservable;


    public SettingsRepository(
        DatabaseContext databaseContext,
        IOptions<ServerSettings> options,
        ISubject<Settings> settingsObservable)
    {
        _databaseContext = databaseContext;
        _options = options;
        _settingsObservable = settingsObservable;
    }

    public async Task<Settings?> UpdateSettings(Settings settings)
    {
        await _databaseContext.AddAsync(settings);
        await _databaseContext.SaveChangesAsync();

        _settingsObservable.OnNext(settings);

        return settings;
    }

    public async Task<Settings?> GetSettings()
    {
        return await _databaseContext.Settings.OrderBy(c => c.Id).LastOrDefaultAsync();
    }
}
