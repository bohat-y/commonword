using Commonword.Modules.Puzzles.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commonword.Modules.Puzzles.Persistence;

public sealed class PuzzleConfiguration : IEntityTypeConfiguration<Puzzle>
{
    public void Configure(EntityTypeBuilder<Puzzle> builder)
    {
        builder.ToTable("puzzles");

        builder.HasKey(puzzle => puzzle.Id);

        builder.Property(puzzle => puzzle.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(puzzle => puzzle.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(puzzle => puzzle.ImportedAt)
            .HasColumnName("imported_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(puzzle => puzzle.IsDaily)
            .HasColumnName("is_daily")
            .HasColumnType("boolean")
            .IsRequired();

        builder.Property(puzzle => puzzle.Data)
            .HasColumnName("data")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(puzzle => puzzle.DataPrivate)
            .HasColumnName("data_private")
            .HasColumnType("jsonb");

        builder.HasIndex(puzzle => puzzle.ImportedAt);
        builder.HasIndex(puzzle => puzzle.IsDaily);
    }
}
