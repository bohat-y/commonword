using Commonword.Modules.Telemetry.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commonword.Modules.Telemetry.Persistence;

public sealed class TelemetryEventConfiguration : IEntityTypeConfiguration<TelemetryEvent>
{
    public void Configure(EntityTypeBuilder<TelemetryEvent> builder)
    {
        builder.ToTable("telemetry_events");

        builder.HasKey(eventRow => eventRow.Id);

        builder.Property(eventRow => eventRow.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(eventRow => eventRow.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(eventRow => eventRow.Client)
            .HasColumnName("client")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(eventRow => eventRow.PlayerId)
            .HasColumnName("player_id")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(eventRow => eventRow.Type)
            .HasColumnName("type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(eventRow => eventRow.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(eventRow => eventRow.SessionId)
            .HasColumnName("session_id")
            .HasColumnType("uuid");

        builder.HasIndex(eventRow => eventRow.OccurredAt);
    }
}
