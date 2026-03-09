using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using Microsoft.AspNetCore.Authentication;

namespace Tracker.Helpers;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }

        catch (System.Net.Http.HttpRequestException ex )
        {
            var response = context.Response;
            var pathBase = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : string.Empty;

            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                response.Redirect($"{pathBase}/Account/Login");
                Error.WriteLog("Token Vencido desde el error handler");
                return;
            }
            else if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                Error.WriteLog(ex);
            }
            else
            {
                Error.WriteLog(ex);
               
            }
        }
        catch (Exception error)
        {

            

            var response = context.Response;
            //response.ContentType = "application/json";

            switch (error)
            {
                //case HttpResponseException e:
                //    // custom application error

                //    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                //    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                //    response.Redirect("/Account/Login");
                //    Error.WriteLog("Token Vencido");
                //    return;
                    
                case ApplicationException e:
                    // custom application error
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case KeyNotFoundException e:
                    // not found error
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                default:
                    // unhandled error
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            //var result = JsonSerializer.Serialize(new { message = error?.Message });
            //await response.WriteAsync(result);
            //Error.WriteLog(result);

            Error.WriteLog(error);
            var pathBase = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : string.Empty;
            response.Redirect($"{pathBase}/Home/Error");
            }
    }
}
