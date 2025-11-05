using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gml.Domains.Auth;
using Gml.Domains.Repositories;
using Gml.Domains.User;
using Gml.Web.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Core.Repositories;

public class RbacRepository : IRbacRepository
{
    private readonly DatabaseContext _db;

    public RbacRepository(DatabaseContext db)
    {
        _db = db;
    }

    // Users
    public Task<List<DbUser>> GetAllUsersAsync()
    {
        return _db.Users.AsNoTracking().ToListAsync();
    }

    // Roles/Permissions for user
    public Task<List<Role>> GetUserRolesAsync(int userId)
    {
        return _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<List<Permission>> GetPermissionsByRoleIdsAsync(IReadOnlyCollection<int> roleIds)
    {
        if (roleIds == null || roleIds.Count == 0)
            return Task.FromResult(new List<Permission>());

        return _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<List<Permission>> GetPermissionsByRoleIdAsync(int roleId)
    {
        return _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .AsNoTracking()
            .ToListAsync();
    }

    // Roles CRUD
    public Task<List<Role>> GetAllRolesAsync()
    {
        return _db.Roles.AsNoTracking().ToListAsync();
    }

    public Task<Role?> GetRoleByIdAsync(int id)
    {
        return _db.Roles.FirstOrDefaultAsync(r => r.Id == id);
    }

    public Task<bool> RoleExistsByNameAsync(string name)
    {
        return _db.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower());
    }

    public async Task<Role> CreateRoleAsync(string name, string? description)
    {
        var role = new Role { Name = name, Description = description };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return role;
    }

    public async Task<Role?> UpdateRoleAsync(int id, string name, string? description)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return null;
        role.Name = name;
        role.Description = description;
        await _db.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return false;
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return true;
    }

    // Permissions CRUD
    public Task<List<Permission>> GetAllPermissionsAsync()
    {
        return _db.Permissions.AsNoTracking().ToListAsync();
    }

    public Task<Permission?> GetPermissionByIdAsync(int id)
    {
        return _db.Permissions.FirstOrDefaultAsync(p => p.Id == id);
    }

    public Task<bool> PermissionExistsByNameAsync(string name)
    {
        return _db.Permissions.AnyAsync(p => p.Name == name);
    }

    public async Task<Permission> CreatePermissionAsync(string name, string? description)
    {
        var perm = new Permission { Name = name, Description = description };
        _db.Permissions.Add(perm);
        await _db.SaveChangesAsync();
        return perm;
    }

    public async Task<Permission?> UpdatePermissionAsync(int id, string name, string? description)
    {
        var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id);
        if (perm == null) return null;
        if (perm.IsSystem)
        {
            return null;
        }
        perm.Name = name;
        perm.Description = description;
        await _db.SaveChangesAsync();
        return perm;
    }

    public async Task<bool> DeletePermissionAsync(int id)
    {
        var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id);
        if (perm == null) return false;
        if (perm.IsSystem)
        {
            return false;
        }
        _db.Permissions.Remove(perm);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> IsSystemPermissionAsync(int id)
    {
        return _db.Permissions
            .Where(p => p.Id == id)
            .Select(p => p.IsSystem)
            .FirstOrDefaultAsync();
    }

    // Assignments
    public Task<bool> RoleHasPermissionAsync(int roleId, int permId)
    {
        return _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permId);
    }

    public async Task AssignPermissionToRoleAsync(int roleId, int permId)
    {
        _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
        await _db.SaveChangesAsync();
    }

    public async Task<bool> RemovePermissionFromRoleAsync(int roleId, int permId)
    {
        var link = await _db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permId);
        if (link == null) return false;
        _db.RolePermissions.Remove(link);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> UserHasRoleAsync(int userId, int roleId)
    {
        return _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
    }

    public async Task AssignRoleToUserAsync(int userId, int roleId)
    {
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        await _db.SaveChangesAsync();
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
    {
        var link = await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        if (link == null) return false;
        _db.UserRoles.Remove(link);
        await _db.SaveChangesAsync();
        return true;
    }
}
