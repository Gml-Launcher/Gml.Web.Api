using Gml.Web.Api.Domains.Settings;

namespace Gml.Web.Api.Core.Repositories;

public interface ISettingsRepository
{
    Task<Settings?> UpdateSettings(Settings settings);
    Task<Settings?> GetSettings();
    IObservable<Settings> SettingsUpdated { get; }
}
