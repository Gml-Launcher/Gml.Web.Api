using System.Net;
using Gml.Web.Api.Core.Messages;

namespace Gml.Web.Api.Core.Middlewares;

public class BadRequestExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BadHttpRequestException ex) when (ex.Message.StartsWith("Implicit body inferred for parameter"))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            await context.Response.WriteAsJsonAsync(ResponseMessage.Create("Тело запроса не может быть пустым",
                HttpStatusCode.BadRequest));
        }
    }
}