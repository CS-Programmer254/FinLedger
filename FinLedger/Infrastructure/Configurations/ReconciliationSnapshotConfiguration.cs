using FinLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinLedger.Infrastructure.Configurations;

public sealed class ReconciliationSnapshotConfiguration : IEntityTypeConfiguration<ReconciliationSnapshot>
{
    public void Configure(EntityTypeBuilder<ReconciliationSnapshot> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.CreatedAt)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.CustomerBalance)
            .HasPrecision(15, 2);

        builder.Property(r => r.ClearingBalance)
            .HasPrecision(15, 2);

        builder.Property(r => r.MerchantBalance)
            .HasPrecision(15, 2);

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        builder.HasIndex(r => r.CreatedAt);

        builder.ToTable("ReconciliationSnapshots");
    }
}
