using Microsoft.EntityFrameworkCore;
using Relay.Shared.Boards;
using Relay.Api.Data;

namespace Relay.Api.Cards.Commands;

public sealed record UpdateCardCommand(string Title, string? Description);

public abstract record UpdateCardResult
{
    public sealed record Updated(CardResponse Card) : UpdateCardResult;

    public sealed record Invalid(string Error) : UpdateCardResult;

    public sealed record Missing : UpdateCardResult;
}

public sealed class UpdateCardHandler(AppDbContext db)
{
    public async Task<UpdateCardResult> Handle(
        Guid cardId,
        UpdateCardCommand command,
        CancellationToken cancellationToken)
    {
        if (!CardText.TryNormalize(command.Title, command.Description, out var title, out var description, out var error))
        {
            return new UpdateCardResult.Invalid(error!);
        }

        var card = await db.Cards.FirstOrDefaultAsync(card => card.Id == cardId, cancellationToken);

        if (card is null)
        {
            return new UpdateCardResult.Missing();
        }

        card.Title = title;
        card.Description = description;
        card.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new UpdateCardResult.Updated(CreateCardHandler.ToResponse(card));
    }
}
