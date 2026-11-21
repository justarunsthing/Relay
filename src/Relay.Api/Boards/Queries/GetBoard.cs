using Microsoft.EntityFrameworkCore;
using Relay.Api.Data;

namespace Relay.Api.Boards.Queries;

public sealed class GetBoardHandler(AppDbContext db)
{
    public async Task<BoardResponse?> Handle(Guid boardId, CancellationToken cancellationToken)
    {
        var board = await db.Boards
            .AsNoTracking()
            .Include(board => board.Columns.OrderBy(column => column.Order))
            .ThenInclude(column => column.Cards.OrderBy(card => card.Order))
            .FirstOrDefaultAsync(board => board.Id == boardId, cancellationToken);

        return board is null ? null : BoardMapper.ToResponse(board);
    }
}
