using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace Gml.Web.Api.Core.Handlers;

public abstract class SentryHandler : ISentryHandler
{
    public static Stream Stream = Stream.Null;

    public static async Task<IResult> GetMessage(HttpContext context, int projectId)
    {
        var decompressor = new GZipStream(context.Request.Body, CompressionMode.Decompress);
        var test = await new StreamReader(decompressor.BaseStream).ReadLineAsync();

        return Results.Empty;
    }
}
