namespace Sonic.Application.Common.Errors;

public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }

    public ApiException(string message, int statusCode, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
