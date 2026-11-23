using Relay.Shared.Boards;
using Relay.Shared.Cards;
using Relay.Shared.Hubs;

namespace Relay.Client.Boards;

public static class BoardView
{
    public static MoveCardResponse? OptimisticMove(BoardResponse board, Guid cardId, Guid columnId, int index)
    {
        var source = board.Columns.FirstOrDefault(column => column.Cards.Any(card => card.Id == cardId));
        var target = board.Columns.FirstOrDefault(column => column.Id == columnId);

        if (source is null || target is null)
        {
            return null;
        }

        var sourceCards = source.Cards.OrderBy(card => card.Order).ToList();
        var card = sourceCards.First(item => item.Id == cardId);
        sourceCards.Remove(card);

        List<CardResponse> targetCards;

        if (source.Id == target.Id)
        {
            targetCards = sourceCards;
        }
        else
        {
            targetCards = target.Cards.OrderBy(item => item.Order).ToList();
        }

        targetCards.Insert(Math.Clamp(index, 0, targetCards.Count), card);

        var orders = Orders(source.Id, sourceCards);

        if (!ReferenceEquals(sourceCards, targetCards))
        {
            orders.AddRange(Orders(target.Id, targetCards));
        }

        return new MoveCardResponse(cardId, source.Id, target.Id, orders);
    }

    public static BoardResponse ApplyOrders(BoardResponse board, IReadOnlyList<CardOrderResponse> orders)
    {
        var cards = board.Columns.SelectMany(column => column.Cards).ToDictionary(card => card.Id);
        var columnOf = new Dictionary<Guid, Guid>();

        foreach (var column in board.Columns)
        {
            foreach (var card in column.Cards)
            {
                columnOf[card.Id] = column.Id;
            }
        }

        var orderOf = new Dictionary<Guid, int>();

        foreach (var update in orders)
        {
            columnOf[update.CardId] = update.ColumnId;
            orderOf[update.CardId] = update.Order;
        }

        return board with
        {
            Columns = board.Columns.Select(column => column with
            {
                Cards = cards.Values
                    .Where(card => columnOf[card.Id] == column.Id)
                    .Select(card => orderOf.TryGetValue(card.Id, out var order) ? card with { Order = order } : card)
                    .OrderBy(card => card.Order)
                    .ToList()
            }).ToList()
        };
    }

    public static BoardResponse Add(BoardResponse board, CardAdded added)
    {
        if (added.BoardId != board.Id || board.Columns.All(column => column.Id != added.ColumnId))
        {
            return board;
        }

        return board with
        {
            Columns = Place(board, added.ColumnId, added.Card).ToList()
        };
    }

    public static BoardResponse Update(BoardResponse board, CardUpdated updated)
    {
        if (updated.BoardId != board.Id || board.Columns.All(column => column.Id != updated.ColumnId))
        {
            return board;
        }

        return board with
        {
            Columns = Place(board, updated.ColumnId, updated.Card).ToList()
        };
    }

    public static BoardResponse Delete(BoardResponse board, CardDeleted deleted)
    {
        if (deleted.BoardId != board.Id)
        {
            return board;
        }

        var orderOf = deleted.Orders.ToDictionary(order => order.CardId, order => order.Order);

        return board with
        {
            Columns = board.Columns.Select(column => column with
            {
                Cards = column.Cards
                    .Where(card => card.Id != deleted.CardId)
                    .Select(card => orderOf.TryGetValue(card.Id, out var order) ? card with { Order = order } : card)
                    .OrderBy(card => card.Order)
                    .ToList()
            }).ToList()
        };
    }

    private static IEnumerable<ColumnResponse> Place(BoardResponse board, Guid columnId, CardResponse card)
    {
        foreach (var column in board.Columns)
        {
            var cards = column.Cards.Where(item => item.Id != card.Id).ToList();

            if (column.Id == columnId)
            {
                cards.Add(card);
            }

            yield return column with { Cards = cards.OrderBy(item => item.Order).ToList() };
        }
    }

    private static List<CardOrderResponse> Orders(Guid columnId, List<CardResponse> cards)
    {
        var orders = new List<CardOrderResponse>(cards.Count);

        for (var index = 0; index < cards.Count; index++)
        {
            orders.Add(new CardOrderResponse(cards[index].Id, columnId, index));
        }

        return orders;
    }
}
