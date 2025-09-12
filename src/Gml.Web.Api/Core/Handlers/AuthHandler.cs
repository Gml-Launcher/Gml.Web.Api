using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Repositories;
using Gml.Web.Api.Dto.Auth;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.User;
using GmlCore.Interfaces;
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
        AccessTokenService tokenService,
        IRefreshTokenRepository refreshRepo,
        Gml.Web.Api.Core.Options.ServerSettings settings,
        DatabaseContext db)
    {
        if (appContext.Settings.RegistrationIsEnabled == false)
            return Results.BadRequest(ResponseMessage.Create("Регистрация для новых пользователей запрещена",
                HttpStatusCode.BadRequest));

        var result = await validator.ValidateAsync(createDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var existing = await userRepository.CheckExistUser(createDto.Login, createDto.Email);

        if (existing is not null)
            return Results.BadRequest(ResponseMessage.Create("Пользователь с указанными данными уже существует",
                HttpStatusCode.BadRequest));

        var user = await userRepository.CreateUser(createDto.Email, createDto.Login, createDto.Password);

        // Ensure Admin role exists and assign to new user
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            adminRole = new Gml.Web.Api.Domains.Auth.Role { Name = "Admin", Description = "System administrator" };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }
        // Link user to Admin if not already
        var hasLink = await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == adminRole.Id);
        if (!hasLink)
        {
            db.UserRoles.Add(new Gml.Web.Api.Domains.Auth.UserRole { UserId = user.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync();
        }

        // Load roles and permissions
        var roles = await db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.Role.Name).ToListAsync();
        var permissions = await db.RolePermissions
            .Where(rp => db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        // Generate JWT pair
        var accessToken = tokenService.GenerateAccessToken(user.Id, roles, permissions);
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
        AccessTokenService tokenService,
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
        var accessToken = tokenService.GenerateAccessToken(user.Id, rolesSignin, permsSignin);
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
        AccessTokenService tokenService,
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
        var newAccess = tokenService.GenerateAccessToken(stored.UserId, rolesRefresh, permsRefresh);
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
}
