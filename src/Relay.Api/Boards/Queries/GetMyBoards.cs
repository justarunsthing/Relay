using Microsoft.EntityFrameworkCore;
using Relay.Api.Data;
using Relay.Shared.Boards;

namespace Relay.Api.Boards.Queries;

public sealed class GetMyBoardsHandler(AppDbContext db)
{
    public async Task<IReadOnlyList<BoardSummaryResponse>> Handle(string userId, CancellationToken cancellationToken)
    {
        return await db.Boards
            .AsNoTracking()
            .Where(board => board.CreatedByUserId == userId)
            .OrderByDescending(board => board.CreatedAt)
            .Select(board => new BoardSummaryResponse(board.Id, board.Name, board.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
