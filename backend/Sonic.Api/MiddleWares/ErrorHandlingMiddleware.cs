using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Sonic.Application.Common.Errors;

namespace Sonic.Api.MiddleWares;

public sealed class ErrorHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            // Your first-class application error type
            _logger.LogWarning(ex, "Handled ApiException {Code} for {Method} {Path}",
                ex.ErrorCode, context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context,
                status: ex.StatusCode,
                title: "Error",
                detail: ex.Message,
                code: ex.ErrorCode!);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Forbidden for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context,
                status: StatusCodes.Status403Forbidden,
                title: "Error",
                detail: ex.Message,
                code: "auth.forbidden");
        }
        catch (ArgumentException ex)
        {
            await WriteProblemAsync(context,
                status: StatusCodes.Status400BadRequest,
                title: "Error",
                detail: ex.Message,
                code: "request.invalid");
        }
        catch (InvalidOperationException ex)
        {
            // Many domain guards throw InvalidOperationException; do NOT turn these into 500s
            await WriteProblemAsync(context,
                status: StatusCodes.Status400BadRequest,
                title: "Error",
                detail: ex.Message,
                code: "request.invalid_operation");
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            await WriteProblemAsync(context,
                status: StatusCodes.Status409Conflict,
                title: "Error",
                detail: "Duplicate key error.",
                code: "db.duplicate_key");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context,
                status: StatusCodes.Status500InternalServerError,
                title: "An error occurred while processing your request.",
                detail: "Unexpected server error.",
                code: "server.error");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string title,
        string detail,
        string code)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

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
                _ => "https://tools.ietf.org/html/rfc9110"
            }
        };

        problem.Extensions["code"] = code;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
