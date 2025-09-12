using System.Collections.Generic;
using System.Threading.Tasks;
using Gml.Web.Api.Domains.Auth;

namespace Gml.Web.Api.Domains.Repositories;

public interface IRbacRepository
{
    // Users
    Task<List<User.User>> GetAllUsersAsync();

    // User roles/permissions
    Task<List<Role>> GetUserRolesAsync(int userId);
    Task<List<Permission>> GetPermissionsByRoleIdsAsync(IReadOnlyCollection<int> roleIds);
    Task<List<Permission>> GetPermissionsByRoleIdAsync(int roleId);

    // Roles CRUD
    Task<List<Role>> GetAllRolesAsync();
    Task<Role?> GetRoleByIdAsync(int id);
    Task<bool> RoleExistsByNameAsync(string name);
    Task<Role> CreateRoleAsync(string name, string? description);
    Task<Role?> UpdateRoleAsync(int id, string name, string? description);
    Task<bool> DeleteRoleAsync(int id);

    // Permissions CRUD
    Task<List<Permission>> GetAllPermissionsAsync();
    Task<Permission?> GetPermissionByIdAsync(int id);
    Task<bool> PermissionExistsByNameAsync(string name);
    Task<Permission> CreatePermissionAsync(string name, string? description);
    Task<Permission?> UpdatePermissionAsync(int id, string name, string? description);
    Task<bool> DeletePermissionAsync(int id);

    // Assignments
    Task<bool> RoleHasPermissionAsync(int roleId, int permId);
    Task AssignPermissionToRoleAsync(int roleId, int permId);
    Task<bool> RemovePermissionFromRoleAsync(int roleId, int permId);

    Task<bool> UserHasRoleAsync(int userId, int roleId);
    Task AssignRoleToUserAsync(int userId, int roleId);
    Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);
}