using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public interface ILauncherUpdateHandler
{
    static abstract Task<IResult> UploadLauncherVersion(HttpContext context, IGmlManager gmlManager);
}
