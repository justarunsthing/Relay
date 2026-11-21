using Relay.Api.Data.Entities;
using Relay.Shared.Boards;

namespace Relay.Api.Boards;

public static class BoardMapper
{
    public static BoardResponse ToResponse(Board board) =>
        new(
            board.Id,
            board.Name,
            board.CreatedAt,
            board.Columns
                .OrderBy(column => column.Order)
                .Select(column => new ColumnResponse(
                    column.Id,
                    column.Title,
                    column.Order,
                    column.Cards
                        .OrderBy(card => card.Order)
                        .Select(card => new CardResponse(
                            card.Id,
                            card.Title,
                            card.Description,
                            card.Order,
                            card.CreatedByUserId,
                            card.UpdatedAt))
                        .ToList()))
                .ToList());
}
