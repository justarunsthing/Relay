using Relay.Shared.Boards;
using Relay.Shared.Cards;
using Relay.Shared.Hubs;

namespace Relay.Api.Hubs;

public interface IBoardClient
{
    Task BoardState(BoardResponse board);

    Task CardAdded(CardAdded card);

    Task CardUpdated(CardUpdated card);

    Task CardDeleted(CardDeleted card);

    Task CardMoved(MoveCardResponse move);
}
