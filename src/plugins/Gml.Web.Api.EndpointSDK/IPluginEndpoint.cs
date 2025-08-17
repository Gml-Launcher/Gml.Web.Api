using System;
using System.Threading.Tasks;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Gml.Web.Api.EndpointSDK;

public interface IPluginEndpoint
{
    Task Execute(HttpContext context, IGmlManager gmlManager, IServiceProvider serviceProvider);
}
