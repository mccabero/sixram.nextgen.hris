namespace Sixram.Api.DTOs;

public sealed class ApiErrorResponse
{
    public string Title { get; init; } = string.Empty;

    public int Status { get; init; }

    public string Detail { get; init; } = string.Empty;

    public string TraceId { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
