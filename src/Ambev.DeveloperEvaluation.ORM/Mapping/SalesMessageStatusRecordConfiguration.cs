using Ambev.DeveloperEvaluation.ORM.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public sealed class SalesMessageStatusRecordConfiguration : IEntityTypeConfiguration<SalesMessageStatusRecord>
{
    public void Configure(EntityTypeBuilder<SalesMessageStatusRecord> builder)
    {
        builder.ToTable("SalesMessageStatuses");

        builder.HasKey(x => x.CorrelationId);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.EventName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.State)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Attempts)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(2048);

        builder.Property(x => x.PayloadJson)
            .HasColumnType("jsonb");
    }
}
