using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Sonic.Application.Common.Errors;

namespace Sonic.Api.MiddleWares;

public sealed class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next = next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;
    private readonly IHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            var code = ex.ErrorCode ?? "app.error";

            _logger.LogWarning(ex,
                "Handled ApiException {StatusCode} {Code} for {Method} {Path}",
                ex.StatusCode, code, context.Request.Method, context.Request.Path);

            await WriteProblemAsync(
                context,
                status: ex.StatusCode,
                title: "Error",
                detail: ex.Message,
                errorCode: code);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex,
                "Forbidden for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(
                context,
                status: StatusCodes.Status403Forbidden,
                title: "Error",
                detail: ex.Message,
                errorCode: "auth.forbidden");
        }
        catch (ArgumentException ex)
        {
            await WriteProblemAsync(
                context,
                status: StatusCodes.Status400BadRequest,
                title: "Error",
                detail: ex.Message,
                errorCode: "request.invalid");
        }
        catch (InvalidOperationException ex)
        {
            // Common for domain/service guard clauses
            await WriteProblemAsync(
                context,
                status: StatusCodes.Status400BadRequest,
                title: "Error",
                detail: ex.Message,
                errorCode: "request.invalid_operation");
        }
        catch (FormatException ex)
        {
            await WriteProblemAsync(
                context,
                status: StatusCodes.Status400BadRequest,
                title: "Error",
                detail: ex.Message,
                errorCode: "request.invalid_format");
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            await WriteProblemAsync(
                context,
                status: StatusCodes.Status409Conflict,
                title: "Error",
                detail: "Duplicate key error.",
                errorCode: "db.duplicate_key");
        }
        catch (MongoAuthenticationException ex)
        {
            _logger.LogError(ex,
                "Mongo authentication failed for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(
                context,
                status: StatusCodes.Status503ServiceUnavailable,
                title: "Error",
                detail: "Database authentication failed.",
                errorCode: "db.auth_failed");
        }
        catch (MongoConnectionException ex)
        {
            _logger.LogError(ex,
                "Mongo connection failed for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(
                context,
                status: StatusCodes.Status503ServiceUnavailable,
                title: "Error",
                detail: "Database connection failed.",
                errorCode: "db.unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var detail = _env.IsDevelopment()
                ? $"{ex.GetType().Name}: {ex.Message}"
                : "Unexpected server error.";

            await WriteProblemAsync(
                context,
                status: StatusCodes.Status500InternalServerError,
                title: "An error occurred while processing your request.",
                detail: detail,
                errorCode: "server.error",
                exceptionType: _env.IsDevelopment() ? ex.GetType().FullName : null);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string title,
        string detail,
        string errorCode,
        string? exceptionType = null)
    {
        if (context.Response.HasStarted) return;

        var problem = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.TraceIdentifier,
            Type = status switch
            {
                400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
                _ => "https://tools.ietf.org/html/rfc9110"
            }
        };

        // Keep extension key stable as "code" (your API responses already use it)
        problem.Extensions["code"] = errorCode;

        if (!string.IsNullOrWhiteSpace(exceptionType))
            problem.Extensions["exceptionType"] = exceptionType;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
