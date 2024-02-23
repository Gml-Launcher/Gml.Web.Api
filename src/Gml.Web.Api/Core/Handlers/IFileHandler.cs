using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public interface IFileHandler
{
    static abstract Task<IResult> GetFile(
        IGmlManager manager,
        string fileHash);
}