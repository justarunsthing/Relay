using Relay.Api.Data;
using Relay.Api.Data.Entities;

namespace Relay.Api.Boards.Commands;

public sealed record CreateBoardCommand(string Name);

public abstract record CreateBoardResult
{
    public sealed record Created(BoardResponse Board) : CreateBoardResult;

    public sealed record Invalid(string Error) : CreateBoardResult;
}

public sealed class CreateBoardHandler(AppDbContext db)
{
    public const int MaxNameLength = 80;

    public async Task<CreateBoardResult> Handle(
        CreateBoardCommand command,
        string userId,
        CancellationToken cancellationToken)
    {
        var name = command.Name?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            return new CreateBoardResult.Invalid("Name is required.");
        }

        if (name.Length > MaxNameLength)
        {
            return new CreateBoardResult.Invalid($"Name must be {MaxNameLength} characters or fewer.");
        }

        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            Columns =
            {
                new Column { Id = Guid.NewGuid(), Title = "To Do", Order = 0 },
                new Column { Id = Guid.NewGuid(), Title = "In Progress", Order = 1 },
                new Column { Id = Guid.NewGuid(), Title = "Done", Order = 2 }
            }
        };

        db.Boards.Add(board);

        await db.SaveChangesAsync(cancellationToken);

        return new CreateBoardResult.Created(BoardMapper.ToResponse(board));
    }
}
