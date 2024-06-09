using System.Reactive.Subjects;

namespace Gml.Web.Api.Core.Hubs;

public class HubEvents
{
    public ISubject<string> KickUser { get; } = new Subject<string>();
}
