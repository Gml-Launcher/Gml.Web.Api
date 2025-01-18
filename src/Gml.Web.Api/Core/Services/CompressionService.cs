using System.IO.Compression;
using System.Text;

namespace Gml.Web.Api.Core.Services;

public class CompressionService
{
    public static async Task<string> Uncompress(byte[] compressedData)
    {
        using var compressedStream = new MemoryStream(compressedData);
        using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();

        await decompressionStream.CopyToAsync(decompressedStream);

        decompressedStream.Position = 0;
        using var reader = new StreamReader(decompressedStream, Encoding.UTF8);

        return await reader.ReadToEndAsync();
    }
}
