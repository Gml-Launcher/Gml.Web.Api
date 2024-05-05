using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using GmlCore.Interfaces;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Handlers;

public interface IMinecraftHandler
{
    static abstract Task<IResult> GetMetaData(ISystemService systemService, IGmlManager gmlManager,
        IOptions<ServerSettings> options);
}
