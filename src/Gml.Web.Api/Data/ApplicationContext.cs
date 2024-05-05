using System.Reactive.Subjects;
using Gml.Web.Api.Domains.Settings;

namespace Gml.Web.Api.Data;

public class ApplicationContext
{
    private readonly ISubject<Settings> _settingsObservable;

    public ApplicationContext(ISubject<Settings> settingsObservable)
    {
        _settingsObservable = settingsObservable;

        _settingsObservable.Subscribe(settings => Settings = settings);
    }

    public Settings Settings { get; private set; }
}
