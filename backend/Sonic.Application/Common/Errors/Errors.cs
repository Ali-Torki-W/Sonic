namespace Sonic.Application.Common.Errors;

public static class Errors
{
    public static ApiException BadRequest(string message, string? code = null) =>
        new ApiException(message, 400, code);

    public static ApiException Unauthorized(string message, string? code = null) =>
        new ApiException(message, 401, code);

    public static ApiException Forbidden(string message, string? code = null) =>
        new ApiException(message, 403, code);

    public static ApiException NotFound(string message, string? code = null) =>
        new ApiException(message, 404, code);

    public static ApiException Conflict(string message, string? code = null) =>
        new ApiException(message, 409, code);

    public static ApiException Internal(string message, string? code = null) =>
        new ApiException(message, 500, code);
}
