namespace Sixram.Api.Exceptions;

public class ApiException : Exception
{
    public ApiException(string message, int statusCode, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}

public sealed class BadRequestException : ApiException
{
    public BadRequestException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message, StatusCodes.Status400BadRequest, errors)
    {
    }
}

public sealed class UnauthorizedApiException : ApiException
{
    public UnauthorizedApiException(string message)
        : base(message, StatusCodes.Status401Unauthorized)
    {
    }
}

public sealed class ForbiddenApiException : ApiException
{
    public ForbiddenApiException(string message)
        : base(message, StatusCodes.Status403Forbidden)
    {
    }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message)
        : base(message, StatusCodes.Status404NotFound)
    {
    }
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string message)
        : base(message, StatusCodes.Status409Conflict)
    {
    }
}
