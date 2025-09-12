using System;
using System.Linq;
using System.Threading.Tasks;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Auth;
using Gml.Web.Api.Domains.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Core.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DatabaseContext _db;

    public RefreshTokenRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<RefreshToken> CreateAsync(int userId, string tokenHash, DateTime expiresAtUtc)
    {
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public Task<RefreshToken?> FindActiveAsync(int userId, string tokenHash)
    {
        return _db.RefreshTokens
            .Where(x => x.UserId == userId && x.TokenHash == tokenHash && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
            .FirstOrDefaultAsync();
    }

    public async Task RevokeAsync(int userId, string tokenHash)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == userId && x.TokenHash == tokenHash);
        if (token != null && token.RevokedAtUtc == null)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RevokeAllAsync(int userId)
    {
        var tokens = await _db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null).ToListAsync();
        foreach (var t in tokens)
        {
            t.RevokedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }
}
