using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Auth;
using Gml.Web.Api.Domains.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Core.Repositories;

public class ExternalApplicationRepository : IExternalApplicationRepository
{
    private readonly DatabaseContext _db;

    public ExternalApplicationRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<ExternalApplication> CreateAsync(int userId, string name, string tokenHash, List<int> permissionIds)
    {
        var app = new ExternalApplication
        {
            UserId = userId,
            Name = name,
            TokenHash = tokenHash,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ExternalApplications.Add(app);
        await _db.SaveChangesAsync();

        // Add permissions
        foreach (var permId in permissionIds)
        {
            _db.ApplicationPermissions.Add(new ApplicationPermission
            {
                ApplicationId = app.Id,
                PermissionId = permId
            });
        }

        await _db.SaveChangesAsync();

        // Load permissions for return
        await _db.Entry(app)
            .Collection(a => a.ApplicationPermissions)
            .Query()
            .Include(ap => ap.Permission)
            .LoadAsync();

        return app;
    }

    public Task<List<ExternalApplication>> GetByUserIdAsync(int userId)
    {
        return _db.ExternalApplications
            .Where(a => a.UserId == userId)
            .Include(a => a.ApplicationPermissions)
            .ThenInclude(ap => ap.Permission)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<ExternalApplication?> GetByIdAsync(Guid id)
    {
        return _db.ExternalApplications
            .Include(a => a.ApplicationPermissions)
            .ThenInclude(ap => ap.Permission)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public Task<ExternalApplication?> GetByTokenHashAsync(string tokenHash)
    {
        return _db.ExternalApplications
            .Include(a => a.ApplicationPermissions)
            .ThenInclude(ap => ap.Permission)
            .FirstOrDefaultAsync(a => a.TokenHash == tokenHash);
    }

    public async Task<bool> DeleteAsync(Guid id, int userId)
    {
        var app = await _db.ExternalApplications
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (app == null)
            return false;

        _db.ExternalApplications.Remove(app);
        await _db.SaveChangesAsync();
        return true;
    }
}
