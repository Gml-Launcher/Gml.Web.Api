using System;
using System.Reflection;

namespace Gml.Web.Api.Domains.Exceptions;

public class VersionNotLoadedException(string message) : Exception
{
    public string InnerExceptionMessage { get; set; } = message;
}
