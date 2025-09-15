using System.Net;
using System.Linq;
using AutoMapper;
using Gml.Web.Api.Data;
using Gml.Web.Api.Dto.Auth;
using Gml.Web.Api.Dto.Messages;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Core.Handlers;

public static class RolesHandler
{
    public static async Task<IResult> GetUsersRbac(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, IMapper mapper)
    {
        var users = await repo.GetAllUsersAsync();

        var result = new List<UserRolesPermissionsDto>(users.Count);

        foreach (var u in users)
        {
            // Load roles as entities and map via AutoMapper
            var roleEntities = await repo.GetUserRolesAsync(u.Id);

            var roles = mapper.Map<List<RoleDto>>(roleEntities)
                .DistinctBy(r => r.Id)
                .ToList();

            var roleIds = roleEntities.Select(r => r.Id).ToList();

            // Load permissions via roles and map via AutoMapper
            var permEntities = await repo.GetPermissionsByRoleIdsAsync(roleIds);

            var perms = mapper.Map<List<PermissionDto>>(permEntities)
                .DistinctBy(p => p.Id)
                .ToList();

            result.Add(new UserRolesPermissionsDto
            {
                Id = u.Id,
                Login = u.Login,
                Email = u.Email,
                Roles = roles,
                Permissions = perms
            });
        }

        return Results.Ok(ResponseMessage.Create(result, "Пользователи с ролями и правами", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetRolesWithPermissions(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, IMapper mapper)
    {
        var roles = await repo.GetAllRolesAsync();

        var result = new List<RoleWithPermissionsDto>(roles.Count);

        foreach (var r in roles)
        {
            var permEntities = await repo.GetPermissionsByRoleIdAsync(r.Id);

            var perms = mapper.Map<List<PermissionDto>>(permEntities)
                .DistinctBy(p => p.Id)
                .ToList();

            result.Add(new RoleWithPermissionsDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Permissions = perms
            });
        }

        return Results.Ok(ResponseMessage.Create(result, "Роли с правами", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetRoles(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, IMapper mapper)
    {
        var roles = await repo.GetAllRolesAsync();
        var dto = mapper.Map<List<RoleDto>>(roles);
        return Results.Ok(ResponseMessage.Create(dto, "Список ролей", HttpStatusCode.OK));
    }

    public static async Task<IResult> CreateRole(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, IMapper mapper, RoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Results.BadRequest(ResponseMessage.Create("Название роли обязательно", HttpStatusCode.BadRequest));
        var exists = await repo.RoleExistsByNameAsync(dto.Name);
        if (exists)
            return Results.BadRequest(ResponseMessage.Create("Роль уже существует", HttpStatusCode.BadRequest));
        var role = await repo.CreateRoleAsync(dto.Name, dto.Description);
        var result = mapper.Map<RoleDto>(role);
        return Results.Ok(ResponseMessage.Create(result, "Роль создана", HttpStatusCode.OK));
    }

    public static async Task<IResult> UpdateRole(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, IMapper mapper, int id, RoleDto dto)
    {
        var role = await repo.UpdateRoleAsync(id, dto.Name, dto.Description);
        if (role == null)
            return Results.NotFound(ResponseMessage.Create("Роль не найдена", HttpStatusCode.NotFound));
        var result = mapper.Map<RoleDto>(role);
        return Results.Ok(ResponseMessage.Create(result, "Роль обновлена", HttpStatusCode.OK));
    }

    public static async Task<IResult> DeleteRole(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, int id)
    {
        // Prevent deleting Admin role
        var role = await repo.GetRoleByIdAsync(id);
        if (role == null)
            return Results.NotFound(ResponseMessage.Create("Роль не найдена", HttpStatusCode.NotFound));
        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(ResponseMessage.Create("Нельзя удалять роль Администратора", HttpStatusCode.BadRequest));

        var ok = await repo.DeleteRoleAsync(id);
        if (!ok)
            return Results.NotFound(ResponseMessage.Create("Роль не найдена", HttpStatusCode.NotFound));
        return Results.Ok(ResponseMessage.Create("Роль удалена", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetPermissions(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, IMapper mapper)
    {
        var perms = await repo.GetAllPermissionsAsync();
        var dto = mapper.Map<List<PermissionDto>>(perms);
        return Results.Ok(ResponseMessage.Create(dto, "Список прав", HttpStatusCode.OK));
    }

    public static async Task<IResult> CreatePermission(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, PermissionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Results.BadRequest(ResponseMessage.Create("Название права обязательно", HttpStatusCode.BadRequest));
        var exists = await repo.PermissionExistsByNameAsync(dto.Name);
        if (exists)
            return Results.BadRequest(ResponseMessage.Create("Право уже существует", HttpStatusCode.BadRequest));
        var perm = await repo.CreatePermissionAsync(dto.Name, dto.Description);
        dto.Id = perm.Id;
        return Results.Ok(ResponseMessage.Create(dto, "Право создано", HttpStatusCode.OK));
    }

    public static async Task<IResult> UpdatePermission(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, int id, PermissionDto dto)
    {
        var existing = await repo.GetPermissionByIdAsync(id);
        if (existing == null)
            return Results.NotFound(ResponseMessage.Create("Право не найдено", HttpStatusCode.NotFound));

        var isSystem = await repo.IsSystemPermissionAsync(id);
        if (isSystem)
            return Results.BadRequest(ResponseMessage.Create("Нельзя изменять системные права", HttpStatusCode.BadRequest));

        var perm = await repo.UpdatePermissionAsync(id, dto.Name, dto.Description);
        if (perm == null)
            return Results.NotFound(ResponseMessage.Create("Право не найдено", HttpStatusCode.NotFound));
        dto.Id = id;
        return Results.Ok(ResponseMessage.Create(dto, "Право обновлено", HttpStatusCode.OK));
    }

    public static async Task<IResult> DeletePermission(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, int id)
    {
        var perm = await repo.GetPermissionByIdAsync(id);
        if (perm == null)
            return Results.NotFound(ResponseMessage.Create("Право не найдено", HttpStatusCode.NotFound));

        // Check system flag via repository implementation (shadow property)
        bool isSystem = await repo.IsSystemPermissionAsync(id);

        if (isSystem)
            return Results.BadRequest(ResponseMessage.Create("Нельзя удалять системные права", HttpStatusCode.BadRequest));

        var ok = await repo.DeletePermissionAsync(id);
        if (!ok)
            return Results.NotFound(ResponseMessage.Create("Право не найдено", HttpStatusCode.NotFound));
        return Results.Ok(ResponseMessage.Create("Право удалено", HttpStatusCode.OK));
    }

    public static async Task<IResult> AssignPermissionToRole(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, int roleId, int permId)
    {
        // Prevent modifying permissions of Admin role
        var role = await repo.GetRoleByIdAsync(roleId);
        if (role != null && string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(ResponseMessage.Create("Нельзя менять права роли Администратора", HttpStatusCode.BadRequest));

        var exists = await repo.RoleHasPermissionAsync(roleId, permId);
        if (exists)
            return Results.Ok(ResponseMessage.Create("Право уже назначено на роль", HttpStatusCode.OK));
        await repo.AssignPermissionToRoleAsync(roleId, permId);
        return Results.Ok(ResponseMessage.Create("Право назначено на роль", HttpStatusCode.OK));
    }

    public static async Task<IResult> UnassignPermissionFromRole(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, int roleId, int permId)
    {
        // Prevent modifying permissions of Admin role
        var role = await repo.GetRoleByIdAsync(roleId);
        if (role != null && string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(ResponseMessage.Create("Нельзя менять права роли Администратора", HttpStatusCode.BadRequest));

        var ok = await repo.RemovePermissionFromRoleAsync(roleId, permId);
        if (!ok)
            return Results.NotFound(ResponseMessage.Create("Связка роль-право не найдена", HttpStatusCode.NotFound));
        return Results.Ok(ResponseMessage.Create("Право снято с роли", HttpStatusCode.OK));
    }

    public static async Task<IResult> AssignRoleToUser(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, int userId, int roleId)
    {
        var exists = await repo.UserHasRoleAsync(userId, roleId);
        if (exists)
            return Results.Ok(ResponseMessage.Create("Роль уже назначена пользователю", HttpStatusCode.OK));
        await repo.AssignRoleToUserAsync(userId, roleId);
        return Results.Ok(ResponseMessage.Create("Роль назначена пользователю", HttpStatusCode.OK));
    }

    public static async Task<IResult> UnassignRoleFromUser(Gml.Web.Api.Domains.Repositories.IRbacRepository repo, int userId, int roleId)
    {
        // Prevent removing Admin role from any user
        var role = await repo.GetRoleByIdAsync(roleId);
        if (role != null && string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(ResponseMessage.Create("Нельзя снять роль Администратора", HttpStatusCode.BadRequest));

        var ok = await repo.RemoveRoleFromUserAsync(userId, roleId);
        if (!ok)
            return Results.NotFound(ResponseMessage.Create("Связка пользователь-роль не найдена", HttpStatusCode.NotFound));
        return Results.Ok(ResponseMessage.Create("Роль снята с пользователя", HttpStatusCode.OK));
    }
}
