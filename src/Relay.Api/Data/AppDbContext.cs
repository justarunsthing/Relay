using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Relay.Api.Data.Entities;

namespace Relay.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Board> Boards => Set<Board>();

    public DbSet<Column> Columns => Set<Column>();

    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Board>()
            .HasMany(board => board.Columns)
            .WithOne(column => column.Board)
            .HasForeignKey(column => column.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Column>(column =>
        {
            column.HasIndex(entity => entity.BoardId);

            column.HasMany(entity => entity.Cards)
                .WithOne(card => card.Column)
                .HasForeignKey(card => card.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Card>()
            .HasIndex(card => card.ColumnId);
    }
}
