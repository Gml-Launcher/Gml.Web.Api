using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using AutoMapper;
using Gml.Web.Api.Domains.Repositories;
using Gml.Web.Api.Dto.Auth;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces.Auth;
using Microsoft.AspNetCore.Http;

namespace Gml.Web.Api.Core.Handlers;

public static class ExternalApplicationHandler
{
    public static async Task<IResult> CreateApplication(
        HttpContext httpContext,
        IExternalApplicationRepository repo,
        IAccessTokenService tokenService,
        IMapper mapper,
        ExternalApplicationCreateDto dto)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Results.BadRequest(ResponseMessage.Create("Название приложения обязательно", HttpStatusCode.BadRequest));
        }

        // Generate token
        var token = tokenService.GenerateRefreshToken();
        var tokenHash = tokenService.HashRefreshToken(token);

        var app = await repo.CreateAsync(userId, dto.Name, tokenHash, dto.PermissionIds);

        var result = new ExternalApplicationReadDto
        {
            Id = app.Id,
            Name = app.Name,
            Token = token, // Return plain token only once
            CreatedAtUtc = app.CreatedAtUtc,
            Permissions = mapper.Map<List<PermissionDto>>(app.ApplicationPermissions.Select(ap => ap.Permission).ToList())
        };

        return Results.Ok(ResponseMessage.Create(result, "Приложение создано успешно", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetUserApplications(
        HttpContext httpContext,
        IExternalApplicationRepository repo,
        IMapper mapper)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var apps = await repo.GetByUserIdAsync(userId);

        var result = apps.Select(app => new ExternalApplicationListDto
        {
            Id = app.Id,
            Name = app.Name,
            CreatedAtUtc = app.CreatedAtUtc,
            Permissions = mapper.Map<List<PermissionDto>>(app.ApplicationPermissions.Select(ap => ap.Permission).ToList())
        }).ToList();

        return Results.Ok(ResponseMessage.Create(result, "Список приложений", HttpStatusCode.OK));
    }

    public static async Task<IResult> DeleteApplication(
        HttpContext httpContext,
        IExternalApplicationRepository repo,
        Guid id)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var success = await repo.DeleteAsync(id, userId);

        if (!success)
        {
            return Results.NotFound(ResponseMessage.Create("Приложение не найдено", HttpStatusCode.NotFound));
        }

        return Results.Ok(ResponseMessage.Create("Приложение удалено", HttpStatusCode.OK));
    }
}
