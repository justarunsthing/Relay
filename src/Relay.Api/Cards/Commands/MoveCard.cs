using Microsoft.EntityFrameworkCore;
using Relay.Api.Data;
using Relay.Api.Data.Entities;
using Relay.Api.Hubs;
using Relay.Shared.Cards;

namespace Relay.Api.Cards.Commands;

public abstract record MoveCardResult
{
    public sealed record Moved(MoveCardResponse Move) : MoveCardResult;

    public sealed record Invalid(string Error) : MoveCardResult;

    public sealed record Missing : MoveCardResult;
}

public sealed class MoveCardHandler(AppDbContext db, BoardBroadcaster boards)
{
    public async Task<MoveCardResult> Handle(
        Guid cardId,
        MoveCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = await db.Cards
            .Include(entity => entity.Column)
            .FirstOrDefaultAsync(entity => entity.Id == cardId, cancellationToken);

        if (card is null)
        {
            return new MoveCardResult.Missing();
        }

        var target = await db.Columns
            .FirstOrDefaultAsync(column => column.Id == request.ColumnId, cancellationToken);

        if (target is null)
        {
            return new MoveCardResult.Missing();
        }

        if (card.Column.BoardId != target.BoardId)
        {
            return new MoveCardResult.Invalid("The column is on a different board.");
        }

        var boardId = card.Column.BoardId;
        var fromColumnId = card.ColumnId;
        var source = await OrderedCards(fromColumnId, cancellationToken);

        source.Remove(card);

        List<Card> targetCards;

        if (fromColumnId == target.Id)
        {
            targetCards = source;
        }
        else
        {
            targetCards = await OrderedCards(target.Id, cancellationToken);
            card.ColumnId = target.Id;
        }

        var index = Math.Clamp(request.Order, 0, targetCards.Count);
        targetCards.Insert(index, card);
        card.UpdatedAt = DateTimeOffset.UtcNow;

        CardOrder.Rewrite(source);

        if (!ReferenceEquals(source, targetCards))
        {
            CardOrder.Rewrite(targetCards);
        }

        await db.SaveChangesAsync(cancellationToken);

        var orders = source
            .Concat(ReferenceEquals(source, targetCards) ? [] : targetCards)
            .Select(entity => new CardOrderResponse(entity.Id, entity.ColumnId, entity.Order))
            .ToList();

        var move = new MoveCardResponse(card.Id, fromColumnId, target.Id, orders);

        await boards.CardMoved(boardId, move);

        return new MoveCardResult.Moved(move);
    }

    private async Task<List<Card>> OrderedCards(Guid columnId, CancellationToken cancellationToken) =>
        await db.Cards
            .Where(card => card.ColumnId == columnId)
            .OrderBy(card => card.Order)
            .ToListAsync(cancellationToken);
}
