using System.Text;

namespace Gml.Web.Api.Core.Services;

public class AccessTokenService
{
    public static string Generate(string login, string secretKey)
    {
        var timestamp = DateTime.Now.Ticks.ToString();
        var guidPart1 = Guid.NewGuid().ToString();
        var guidPart2 = Guid.NewGuid().ToString();

        var textBytes = Encoding.UTF8.GetBytes(string.Join(login, timestamp, secretKey, guidPart1, guidPart2));
        return Convert.ToBase64String(textBytes);
    }
}
