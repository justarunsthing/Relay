using Microsoft.EntityFrameworkCore;
using Relay.Api.Data;

namespace Relay.Api.Cards.Commands;

public abstract record DeleteCardResult
{
    public sealed record Deleted : DeleteCardResult;

    public sealed record Missing : DeleteCardResult;
}

public sealed class DeleteCardHandler(AppDbContext db)
{
    public async Task<DeleteCardResult> Handle(Guid cardId, CancellationToken cancellationToken)
    {
        var card = await db.Cards.FirstOrDefaultAsync(entity => entity.Id == cardId, cancellationToken);

        if (card is null)
        {
            return new DeleteCardResult.Missing();
        }

        var columnId = card.ColumnId;

        db.Cards.Remove(card);

        var remaining = await db.Cards
            .Where(entity => entity.ColumnId == columnId && entity.Id != cardId)
            .OrderBy(entity => entity.Order)
            .ToListAsync(cancellationToken);

        CardOrder.Rewrite(remaining);

        await db.SaveChangesAsync(cancellationToken);

        return new DeleteCardResult.Deleted();
    }
}
