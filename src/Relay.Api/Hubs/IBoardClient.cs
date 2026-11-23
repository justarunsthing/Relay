using Relay.Shared.Boards;
using Relay.Shared.Cards;
using Relay.Shared.Hubs;

namespace Relay.Api.Hubs;

public interface IBoardClient
{
    Task BoardState(BoardResponse board, IReadOnlyList<BoardUser> users);

    Task CardAdded(CardAdded card);

    Task CardUpdated(CardUpdated card);

    Task CardDeleted(CardDeleted card);

    Task CardMoved(MoveCardResponse move);

    Task UserJoined(UserJoined joined);

    Task UserLeft(UserLeft left);
}
