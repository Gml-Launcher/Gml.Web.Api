using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Gml.Web.Api.Domains.User;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Gml.Web.Api.EndpointSDK;

public class EndpointHelper
{
    public async Task<T?> ParseDto<T>(HttpContext context)
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(body);
    }

    public bool IsValidDto<TDto, TValidator>(TDto? dto, out ValidationResult validationResult)
        where TValidator : AbstractValidator<TDto>, new()
        where TDto : class
    {
        if (dto == null)
        {
            validationResult = new ValidationResult(new List<ValidationFailure>
            {
                new ("Dto", "Тело запроса не может быть пустым")
            });

            return false;
        }

        var validator = new TValidator();
        validationResult = validator.Validate(dto ?? throw new Exception());
        return validationResult.IsValid;
    }

    public async Task NotFound(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;

        var content = new ResponseMessage
        {
            Message = message,
            Status = HttpStatusCode.NotFound.ToString(),
            StatusCode = (int)HttpStatusCode.NotFound
        };

        await context.Response.WriteAsync(JsonConvert.SerializeObject(content));
    }

    public async Task Created(HttpContext context, object? data, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Created;

        var content = ResponseMessage.Create(data, message, HttpStatusCode.Created);

        await context.Response.WriteAsync(JsonConvert.SerializeObject(content));
    }

    public async Task Ok(HttpContext context, object? data, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        var content = ResponseMessage.Create(data, message, HttpStatusCode.OK);

        await context.Response.WriteAsync(JsonConvert.SerializeObject(content));
    }

    public async Task BadRequest(HttpContext context, object? data, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var content = ResponseMessage.Create(data, message, HttpStatusCode.BadRequest);

        await context.Response.WriteAsync(JsonConvert.SerializeObject(content));
    }

    public static async Task BadRequest(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var content = new ResponseMessage
        {
            Message = message,
            Status = HttpStatusCode.BadRequest.ToString(),
            StatusCode = (int)HttpStatusCode.BadRequest
        };

        await context.Response.WriteAsync(JsonConvert.SerializeObject(content));
    }

    public static async Task BadRequest(HttpContext context, List<ValidationFailure> resultErrors, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var content = new ResponseMessage
        {
            Message = message,
            Status = HttpStatusCode.BadRequest.ToString(),
            StatusCode = (int)HttpStatusCode.BadRequest,
            Errors = resultErrors.Select(c => c.ErrorMessage)
        };

        await context.Response.WriteAsync(JsonConvert.SerializeObject(content));
    }
}
