using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InventoryWarehouseApi.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        int status = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        if (status == 500) logger.LogError(exception, "An unhandled request exception occurred.");
        else logger.LogInformation("Request failed with status {StatusCode}: {ExceptionMessage}", status, exception.Message);

        ProblemDetails details = new()
        {
            Status = status,
            Title = status switch { 400 => "Validation failed", 404 => "Resource not found", 409 => "Conflict", _ => "An unexpected error occurred" },
            Detail = status == 500 ? "An unexpected error occurred while processing the request." : exception.Message
        };
        if (exception is ValidationException validation)
            details.Extensions["errors"] = validation.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).Distinct().ToArray());
        context.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
    }
}
