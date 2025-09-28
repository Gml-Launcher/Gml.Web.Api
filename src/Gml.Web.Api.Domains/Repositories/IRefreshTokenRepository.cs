using System;
using System.Threading.Tasks;
using Gml.Web.Api.Domains.Auth;

namespace Gml.Web.Api.Domains.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> CreateAsync(int userId, string tokenHash, DateTime expiresAtUtc);
    Task<RefreshToken?> FindActiveAsync(int userId, string tokenHash);
    Task<RefreshToken?> FindActiveByHashAsync(string tokenHash);
    Task RevokeAsync(int userId, string tokenHash);
    Task RevokeAllAsync(int userId);
}
