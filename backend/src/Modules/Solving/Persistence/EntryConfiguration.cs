using Commonword.Modules.Solving.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commonword.Modules.Solving.Persistence;

public sealed class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.ToTable("entries");

        builder.HasKey(entry => new { entry.SessionId, entry.Row, entry.Col });

        builder.Property(entry => entry.SessionId)
            .HasColumnName("session_id")
            .IsRequired();

        builder.Property(entry => entry.Row)
            .HasColumnName("row")
            .IsRequired();

        builder.Property(entry => entry.Col)
            .HasColumnName("col")
            .IsRequired();

        builder.Property(entry => entry.Value)
            .HasColumnName("value")
            .HasColumnType("char(1)")
            .IsRequired();

        builder.Property(entry => entry.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();
    }
}
