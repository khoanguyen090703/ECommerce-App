using ECommerce.Application.Exceptions;

namespace ECommerce.Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(Exception ex)
            {
                context.Response.ContentType = "application/json";

                context.Response.StatusCode = ex switch
                {
                    NotFoundException => 404,
                    ArgumentException => 400,
                    _ => 500
                };

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        error = ex.Message
                    });
            }
        }
    }
}
