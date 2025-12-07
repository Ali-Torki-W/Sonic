using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sonic.Application.Common.Errors;

namespace Sonic.Api.MiddleWares;

public sealed class ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger) : IMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, errorCode, detail) = MapException(ex);

            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}. Returning {StatusCode}.",
                context.Request.Method,
                context.Request.Path,
                statusCode);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "The response has already started, cannot write error response.");
                throw;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode >= 500 ? "Internal server error" : "Error",
                Detail = detail,
                Instance = context.TraceIdentifier
            };

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                problem.Extensions["code"] = errorCode;
            }

            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private static (int StatusCode, string? ErrorCode, string Detail) MapException(Exception ex)
    {
        // Our explicit application errors
        if (ex is ApiException apiEx)
        {
            return (apiEx.StatusCode, apiEx.ErrorCode, apiEx.Message);
        }

        // Basic “bad input” case from our own guards
        if (ex is ArgumentException argEx)
        {
            return (400, null, argEx.Message);
        }

        // Everything else = real server bug
        return (500, null, "An unexpected error occurred while processing your request.");
    }
}
