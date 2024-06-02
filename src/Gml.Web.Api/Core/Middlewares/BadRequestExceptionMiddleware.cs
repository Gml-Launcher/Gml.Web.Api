using System.Net;
using Gml.Web.Api.Dto.Messages;

namespace Gml.Web.Api.Core.Middlewares;

public class BadRequestExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BadHttpRequestException badHttpRequestException) when (badHttpRequestException.Message.StartsWith(
                                                                          "Implicit body inferred for parameter"))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            await context.Response.WriteAsJsonAsync(ResponseMessage.Create("Тело запроса не может быть пустым",
                HttpStatusCode.BadRequest));
        }
        catch (BadHttpRequestException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            await context.Response.WriteAsJsonAsync(ResponseMessage.Create(exception.Message,
                HttpStatusCode.BadRequest));
        }
        catch (IOException ioException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            Console.WriteLine(ioException);
            await context.Response.WriteAsJsonAsync(ResponseMessage.Create(
                "Произошла ошибка при работе с файловой системой. Попробуйте перезапустить сервис для восстановления работы",
                HttpStatusCode.InternalServerError));
        }
    }
}
