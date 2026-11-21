using Relay.Api.Data.Entities;

namespace Relay.Api.Cards;

public static class CardOrder
{
    public static void Rewrite(IReadOnlyList<Card> cards)
    {
        for (var index = 0; index < cards.Count; index++)
        {
            cards[index].Order = index;
        }
    }
}
