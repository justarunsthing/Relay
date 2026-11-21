using Microsoft.EntityFrameworkCore;
using Relay.Api.Boards;
using Relay.Api.Data;
using Relay.Api.Data.Entities;

namespace Relay.Api.Cards.Commands;

public sealed record CreateCardCommand(Guid ColumnId, string Title, string? Description);

public abstract record CreateCardResult
{
    public sealed record Created(CardResponse Card) : CreateCardResult;

    public sealed record Invalid(string Error) : CreateCardResult;

    public sealed record Missing : CreateCardResult;
}

public sealed class CreateCardHandler(AppDbContext db)
{
    public async Task<CreateCardResult> Handle(
        Guid boardId,
        CreateCardCommand command,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!CardText.TryNormalize(command.Title, command.Description, out var title, out var description, out var error))
        {
            return new CreateCardResult.Invalid(error!);
        }

        var columnExists = await db.Columns
            .AnyAsync(column => column.Id == command.ColumnId && column.BoardId == boardId, cancellationToken);

        if (!columnExists)
        {
            return new CreateCardResult.Missing();
        }

        var order = await db.Cards.CountAsync(card => card.ColumnId == command.ColumnId, cancellationToken);
        var card = new Card
        {
            Id = Guid.NewGuid(),
            ColumnId = command.ColumnId,
            Title = title,
            Description = description,
            Order = order,
            CreatedByUserId = userId,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Cards.Add(card);

        await db.SaveChangesAsync(cancellationToken);

        return new CreateCardResult.Created(ToResponse(card));
    }

    internal static CardResponse ToResponse(Card card) =>
        new(card.Id, card.Title, card.Description, card.Order, card.CreatedByUserId, card.UpdatedAt);
}
