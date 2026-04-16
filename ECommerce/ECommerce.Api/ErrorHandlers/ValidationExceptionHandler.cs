using Microsoft.AspNetCore.Diagnostics;

namespace ECommerce.Api.ErrorHandlers
{
    public class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Kiểm tra nếu không phải lỗi Validation thì bỏ qua cho Handler kế tiếp
            if (exception is not FluentValidation.ValidationException fluentException)
            {
                return false;
            }

            logger.LogWarning("Validation failed: {Message}", fluentException.Message);

            var errors = fluentException.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray()
                );

            // Sử dụng ValidationProblemDetails để đúng chuẩn REST của Microsoft
            var response = new
            {
                error = "Validation Failed",
                details = errors
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true; // Xác nhận đã xử lý xong lỗi này
        }
    }
}
