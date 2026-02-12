using Commonword.Modules.Solving.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commonword.Modules.Solving.Persistence;

public sealed class SolveSessionConfiguration : IEntityTypeConfiguration<SolveSession>
{
    public void Configure(EntityTypeBuilder<SolveSession> builder)
    {
        builder.ToTable("solve_sessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(session => session.PuzzleId)
            .HasColumnName("puzzle_id")
            .IsRequired();

        builder.Property(session => session.PlayerId)
            .HasColumnName("player_id")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(session => session.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(session => session.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(session => session.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamptz");

        builder.HasMany(session => session.Entries)
            .WithOne(entry => entry.Session)
            .HasForeignKey(entry => entry.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(session => session.PuzzleId);
        builder.HasIndex(session => session.PlayerId);
    }
}
