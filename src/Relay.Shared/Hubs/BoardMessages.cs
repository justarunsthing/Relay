using Relay.Shared.Boards;
using Relay.Shared.Cards;

namespace Relay.Shared.Hubs;

public sealed record CardAdded(Guid BoardId, Guid ColumnId, CardResponse Card);

public sealed record CardUpdated(Guid BoardId, Guid ColumnId, CardResponse Card);

public sealed record CardDeleted(
    Guid BoardId,
    Guid ColumnId,
    Guid CardId,
    IReadOnlyList<CardOrderResponse> Orders);
