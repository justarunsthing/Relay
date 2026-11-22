using Microsoft.AspNetCore.SignalR;
using Relay.Shared.Boards;
using Relay.Shared.Cards;
using Relay.Shared.Hubs;

namespace Relay.Api.Hubs;

public sealed class BoardBroadcaster(IHubContext<BoardHub, IBoardClient> hub)
{
    public Task CardAdded(Guid boardId, Guid columnId, CardResponse card) =>
        hub.Clients.Group(BoardHub.GroupName(boardId)).CardAdded(new CardAdded(boardId, columnId, card));

    public Task CardUpdated(Guid boardId, Guid columnId, CardResponse card) =>
        hub.Clients.Group(BoardHub.GroupName(boardId)).CardUpdated(new CardUpdated(boardId, columnId, card));

    public Task CardDeleted(
        Guid boardId,
        Guid columnId,
        Guid cardId,
        IReadOnlyList<CardOrderResponse> orders) =>
        hub.Clients.Group(BoardHub.GroupName(boardId))
            .CardDeleted(new CardDeleted(boardId, columnId, cardId, orders));

    public Task CardMoved(Guid boardId, MoveCardResponse move) =>
        hub.Clients.Group(BoardHub.GroupName(boardId)).CardMoved(move);
}
