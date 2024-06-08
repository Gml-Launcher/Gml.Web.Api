using System;

namespace Gml.Web.Api.Domains.User;

public class UserLauncherInfo
{
    public DateTimeOffset ExpiredDate { get; set; }
    public IDisposable Subscription { get; set; }
}
