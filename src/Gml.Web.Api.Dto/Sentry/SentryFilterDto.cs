using System;

namespace Gml.Web.Api.Dto.Sentry;

public class SentryFilterDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
