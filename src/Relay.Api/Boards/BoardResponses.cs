namespace Relay.Api.Boards;

public sealed record BoardSummaryResponse(Guid Id, string Name, DateTimeOffset CreatedAt);

public sealed record BoardResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ColumnResponse> Columns);

public sealed record ColumnResponse(
    Guid Id,
    string Title,
    int Order,
    IReadOnlyList<CardResponse> Cards);

public sealed record CardResponse(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    string CreatedByUserId,
    DateTimeOffset UpdatedAt);

public sealed record BoardRequestError(string Error);
