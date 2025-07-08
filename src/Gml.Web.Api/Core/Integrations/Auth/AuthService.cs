using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Domains.Integrations;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Enums;
using OtpNet;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class AuthService(IAuthServiceFactory authServiceFactory) : IAuthService
{
    public async Task<AuthResult> CheckAuth(string login, string password, AuthType authType, string? totp = null)
    {
        var service = authServiceFactory.CreateAuthService(authType);
        return await service.Auth(login, password, totp);
    }

    public async Task<bool> ValidateTotp(string totp, string? secret)
    {
        if (string.IsNullOrEmpty(secret))
            return false;

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp2fa = new Totp(secretBytes);
            return totp2fa.VerifyTotp(totp, out _);
        }
        catch
        {
            return false;
        }
    }
}
