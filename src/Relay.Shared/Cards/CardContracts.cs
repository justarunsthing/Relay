namespace Relay.Shared.Cards;

public sealed record MoveCardRequest(Guid ColumnId, int Order);

public sealed record CardOrderResponse(Guid CardId, Guid ColumnId, int Order);

public sealed record MoveCardResponse(
    Guid CardId,
    Guid FromColumnId,
    Guid ToColumnId,
    IReadOnlyList<CardOrderResponse> Orders);
