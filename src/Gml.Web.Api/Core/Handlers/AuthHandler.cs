using System.Net;
using System.Security.Claims;
using AutoMapper;
using FluentValidation;
using Gml.Domains.Auth;
using Gml.Domains.Repositories;
using Gml.Dto.Auth;
using Gml.Dto.Messages;
using Gml.Dto.Player;
using Gml.Dto.User;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Core.Handlers;

public class AuthHandler : IAuthHandler
{
    public static async Task<IResult> CreateUser(
        HttpContext httpContext,
        IUserRepository userRepository,
        IValidator<UserCreateDto> validator,
        IMapper mapper,
        UserCreateDto createDto,
        ApplicationContext appContext,
        IAccessTokenService tokenService,
        IRefreshTokenRepository refreshRepo,
        Gml.Web.Api.Core.Options.ServerSettings settings,
        DatabaseContext db)
    {
        if (appContext.Settings.RegistrationIsEnabled == false && !httpContext.User.IsInRole("Admin"))
            return Results.BadRequest(ResponseMessage.Create("Регистрация для новых пользователей запрещена",
                HttpStatusCode.BadRequest));

        var adminRoleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        var hasAdmins = adminRoleEntity != null && await db.UserRoles.AnyAsync(ur => ur.RoleId == adminRoleEntity.Id);

        if (hasAdmins)
        {
            var userPrincipal = httpContext.User;
            var isAuthenticated = userPrincipal?.Identity?.IsAuthenticated == true;
            var isAdmin = userPrincipal?.IsInRole("Admin") == true;
            if (!isAuthenticated || !isAdmin)
            {
                return Results.Forbid();
            }
        }

        var result = await validator.ValidateAsync(createDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var existing = await userRepository.CheckExistUser(createDto.Login, createDto.Email);

        if (existing is not null)
            return Results.BadRequest(ResponseMessage.Create("Пользователь с указанными данными уже существует",
                HttpStatusCode.BadRequest));

        // Decide the role to assign
        string targetRoleName;
        if (!hasAdmins)
        {
            // Bootstrap: ensure Admin role exists and use it regardless of the requested role
            if (adminRoleEntity == null)
            {
                adminRoleEntity = new Role { Name = "Admin", Description = "System administrator" };
                db.Roles.Add(adminRoleEntity);
                await db.SaveChangesAsync();
            }
            targetRoleName = "Admin";
        }
        else
        {
            var requestedRole = httpContext.Request.Query.TryGetValue("role", out var roleVals) ? roleVals.ToString() : null;
            targetRoleName = string.IsNullOrWhiteSpace(requestedRole) ? "Admin" : requestedRole.Trim();
        }

        // Fetch target role (only create Admin implicitly during bootstrap)
        var targetRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName);
        if (targetRole == null)
        {
            return Results.BadRequest(ResponseMessage.Create($"Роль '{targetRoleName}' не найдена", HttpStatusCode.BadRequest));
        }

        // Create user
        var user = await userRepository.CreateUser(createDto.Email, createDto.Login, createDto.Password);

        // Link user to target role
        var hasLink = await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == targetRole.Id);
        if (!hasLink)
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = targetRole.Id });
            await db.SaveChangesAsync();
        }

        // Load roles and permissions
        var roles = await db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.Role.Name).ToListAsync();
        var permissions = await db.RolePermissions
            .Where(rp => db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        // Generate JWT pair for the newly created user
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Login, user.Email, roles, permissions);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshHash = tokenService.HashRefreshToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(settings.RefreshTokenDays);
        await refreshRepo.CreateAsync(user.Id, refreshHash, expiresAt);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt
        };
        httpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        var tokens = new AuthTokensDto
        {
            AccessToken = accessToken,
            ExpiresIn = settings.AccessTokenMinutes * 60,
        };

        return Results.Ok(ResponseMessage.Create(tokens, "Успешная регистрация",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> UserInfo(IGmlManager manager, IMapper mapper, string userName)
    {
        var user = await manager.Users.GetUserByName(userName);

        if (user is null)
        {
            return Results.NotFound(ResponseMessage.Create("Пользователь не найден", HttpStatusCode.BadRequest));
        }

        return Results.Ok(ResponseMessage.Create(mapper.Map<PlayerTextureDto>(user), "Успешная авторизация",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> AuthUser(
        HttpContext httpContext,
        IUserRepository userRepository,
        IValidator<UserAuthDto> validator,
        IMapper mapper,
        UserAuthDto authDto,
        IAccessTokenService tokenService,
        IRefreshTokenRepository refreshRepo,
        Gml.Web.Api.Core.Options.ServerSettings settings,
        DatabaseContext db)
    {
        var result = await validator.ValidateAsync(authDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var user = await userRepository.GetUser(authDto.Login, authDto.Password);

        if (user is null)
            return Results.BadRequest(ResponseMessage.Create("Неверный логин или пароль",
                HttpStatusCode.BadRequest));

        // Compute roles/perms for JWT
        var rolesSignin = await db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.Role.Name).ToListAsync();
        var permsSignin = await db.RolePermissions
            .Where(rp => db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Login, user.Email, rolesSignin, permsSignin);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshHash = tokenService.HashRefreshToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(settings.RefreshTokenDays);
        await refreshRepo.CreateAsync(user.Id, refreshHash, expiresAt);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt
        };
        httpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        var tokens = new AuthTokensDto
        {
            AccessToken = accessToken,
            ExpiresIn = settings.AccessTokenMinutes * 60,
        };

        return Results.Ok(ResponseMessage.Create(tokens, "Успешная авторизация",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> RefreshTokens(
        HttpContext httpContext,
        IAccessTokenService tokenService,
        IRefreshTokenRepository refreshRepo,
        Gml.Web.Api.Core.Options.ServerSettings settings,
        DatabaseContext db)
    {
        var refreshToken = httpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Results.Unauthorized();

        var hash = tokenService.HashRefreshToken(refreshToken);
        var stored = await refreshRepo.FindActiveByHashAsync(hash);
        if (stored is null)
            return Results.Unauthorized();

        // Revoke old token (rotation)
        await refreshRepo.RevokeAsync(stored.UserId, stored.TokenHash);

        // Issue new pair
        var rolesRefresh = await db.UserRoles.Where(ur => ur.UserId == stored.UserId).Select(ur => ur.Role.Name).ToListAsync();
        var permsRefresh = await db.RolePermissions
            .Where(rp => db.UserRoles.Where(ur => ur.UserId == stored.UserId).Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
        var newAccess = tokenService.GenerateAccessToken(stored.UserId, null, null, rolesRefresh, permsRefresh);
        var newRefresh = tokenService.GenerateRefreshToken();
        var newHash = tokenService.HashRefreshToken(newRefresh);
        var expiresAt = DateTime.UtcNow.AddDays(settings.RefreshTokenDays);
        await refreshRepo.CreateAsync(stored.UserId, newHash, expiresAt);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt
        };
        httpContext.Response.Cookies.Append("refreshToken", newRefresh, cookieOptions);

        var tokens = new AuthTokensDto
        {
            AccessToken = newAccess,
            ExpiresIn = settings.AccessTokenMinutes * 60
        };

        return Results.Ok(ResponseMessage.Create(tokens, "Токены обновлены", HttpStatusCode.OK));
    }

    public static Task<IResult> UpdateUser(IUserRepository userRepository, UserUpdateDto userUpdateDto)
    {
        throw new NotImplementedException();
    }

    public static async Task<IResult> DeleteUser(HttpContext httpContext, DatabaseContext db, IRefreshTokenRepository refreshRepo, int userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Results.NotFound(ResponseMessage.Create("Пользователь не найден", HttpStatusCode.NotFound));

        // Check if user has Admin role
        var hasAdminRole = await db.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Admin");

        var currentUserId = int.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == userId)
            return Results.BadRequest(ResponseMessage.Create("Вы не можете удалить свою учетную запись", HttpStatusCode.BadRequest));

        if (hasAdminRole)
            return Results.BadRequest(ResponseMessage.Create("Нельзя удалять пользователя с ролью Admin", HttpStatusCode.BadRequest));

        // Revoke all refresh tokens
        await refreshRepo.RevokeAllAsync(user.Id);

        // Remove role links
        var links = db.UserRoles.Where(ur => ur.UserId == user.Id);
        db.UserRoles.RemoveRange(links);

        // Finally remove user
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return Results.Ok(ResponseMessage.Create("Пользователь удален", HttpStatusCode.OK));
    }
}
