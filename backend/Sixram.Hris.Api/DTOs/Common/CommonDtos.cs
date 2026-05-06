using System.ComponentModel.DataAnnotations;

namespace Sixram.Api.DTOs.Common;

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}

public sealed class LookupOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public Guid? ParentId { get; init; }

    public bool IsActive { get; init; }
}

public sealed class UserOptionDto
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }
}

public abstract class PagedQueryDto
{
    [Range(1, 10_000)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [MaxLength(100)]
    public string Search { get; init; } = string.Empty;
}
