using Microsoft.EntityFrameworkCore;
using Relay.Api.Data;
using Relay.Api.Hubs;
using Relay.Shared.Cards;

namespace Relay.Api.Cards.Commands;

public abstract record DeleteCardResult
{
    public sealed record Deleted : DeleteCardResult;

    public sealed record Missing : DeleteCardResult;
}

public sealed class DeleteCardHandler(AppDbContext db, BoardBroadcaster boards)
{
    public async Task<DeleteCardResult> Handle(Guid cardId, CancellationToken cancellationToken)
    {
        var card = await db.Cards
            .Include(entity => entity.Column)
            .FirstOrDefaultAsync(entity => entity.Id == cardId, cancellationToken);

        if (card is null)
        {
            return new DeleteCardResult.Missing();
        }

        var boardId = card.Column.BoardId;
        var columnId = card.ColumnId;

        db.Cards.Remove(card);

        var remaining = await db.Cards
            .Where(entity => entity.ColumnId == columnId && entity.Id != cardId)
            .OrderBy(entity => entity.Order)
            .ToListAsync(cancellationToken);

        CardOrder.Rewrite(remaining);

        await db.SaveChangesAsync(cancellationToken);

        var orders = remaining
            .Select(entity => new CardOrderResponse(entity.Id, entity.ColumnId, entity.Order))
            .ToList();

        await boards.CardDeleted(boardId, columnId, cardId, orders);

        return new DeleteCardResult.Deleted();
    }
}
