namespace Relay.Api.Data.Entities;

public class Column
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Order { get; set; }

    public Board Board { get; set; } = null!;

    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
