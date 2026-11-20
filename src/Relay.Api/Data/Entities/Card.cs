namespace Relay.Api.Data.Entities;

public class Card
{
    public Guid Id { get; set; }

    public Guid ColumnId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Order { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public Column Column { get; set; } = null!;
}
