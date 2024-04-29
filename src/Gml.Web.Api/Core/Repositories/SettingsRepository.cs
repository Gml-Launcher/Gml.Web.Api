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


    public SettingsRepository(DatabaseContext databaseContext, IOptions<ServerSettings> options)
    {
        _databaseContext = databaseContext;
        _options = options;
    }

    public async Task<Settings?> UpdateSettings(Settings settings)
    {
        await _databaseContext.AddAsync(settings);
        await _databaseContext.SaveChangesAsync();

        return settings;
    }

    public async Task<Settings?> GetSettings()
    {
        return await _databaseContext.Settings.OrderBy(c => c.Id).LastOrDefaultAsync();
    }
}
