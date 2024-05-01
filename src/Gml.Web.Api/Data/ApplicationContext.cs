using System.Reactive.Subjects;
using Gml.Web.Api.Domains.Settings;
using SQLitePCL;

namespace Gml.Web.Api.Data;

public class ApplicationContext
{
    private readonly ISubject<Settings> _settingsObservable;
    public Settings Settings => _settings;
    private Settings _settings;

    public ApplicationContext(ISubject<Settings> settingsObservable)
    {
        _settingsObservable = settingsObservable;

        _settingsObservable.Subscribe(settings => _settings = settings);
    }
}
