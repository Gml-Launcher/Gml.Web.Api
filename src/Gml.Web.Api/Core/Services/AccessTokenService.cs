using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Domains.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gml.Web.Api.Core.Services;

public class AccessTokenService(ServerSettings settings)
{
    public bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecurityKey));

        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false
            };

            var principle = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            return true;
        }
        catch
        {
            // Здесь вы можете обрабатывать исключение, выбрасываемое в случае недействительного токена
            return false;
        }
    }

    public static string Generate(string login, string secretKey)
    {
        var timestamp = DateTime.Now.Ticks.ToString();
        var guidPart1 = Guid.NewGuid().ToString();
        var guidPart2 = Guid.NewGuid().ToString();

        var textBytes = Encoding.UTF8.GetBytes(string.Join(login, timestamp, secretKey, guidPart1, guidPart2));
        return Convert.ToBase64String(textBytes);
    }
}
