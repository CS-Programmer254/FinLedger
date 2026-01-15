using FinLedger.Domain.Aggregates;
using FinLedger.Domain.Entities;
using FinLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinLedger.Infrastructure.Configurations
{
    public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedNever();

            builder.Property(p => p.CreatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .ValueGeneratedOnAdd();

            builder.Property(p => p.RetryCount)
                   .HasDefaultValue(0);

            builder.Property(p => p.Status)
                   .HasConversion(
                        v => v.ToString(),
                        v => Enum.Parse<PaymentStatus>(v))
                   .HasMaxLength(50)
                   .IsRequired();

            // Owned types
            builder.OwnsOne(p => p.MerchantId, nav =>
            {
                nav.Property(m => m.Value)
                   .HasColumnName("MerchantId")
                   .HasMaxLength(36)
                   .IsRequired();

                nav.HasIndex(m => m.Value);
            });

            builder.OwnsOne(p => p.Amount, nav =>
            {
                nav.Property(a => a.Amount).HasColumnName("Amount").IsRequired();
                nav.Property(a => a.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
            });

            builder.OwnsOne(p => p.Reference, nav =>
            {
                nav.Property(r => r.Value)
                   .HasColumnName("Reference")
                   .HasMaxLength(50)
                   .IsRequired();

                nav.HasIndex(r => r.Value).IsUnique();
            });

            // Ledger Entries (owned collection)
            builder.OwnsMany(p => p.LedgerEntries, ledger =>
            {
                ledger.WithOwner().HasForeignKey("PaymentId");
                ledger.HasKey(l => l.Id);

                ledger.Property(l => l.Account)
                      .HasConversion(
                          v => v.ToString(),
                          v => Enum.Parse<LedgerAccount>(v))
                      .HasMaxLength(50)
                      .IsRequired();

                ledger.Property(l => l.Debit).IsRequired();
                ledger.Property(l => l.Credit).IsRequired();
                ledger.Property(l => l.TransactionHash)
                      .HasMaxLength(88)
                      .IsRequired();
                ledger.Property(l => l.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                ledger.ToTable("LedgerEntries");

                ledger.HasIndex(l => l.PaymentId);
                ledger.HasIndex(l => l.Account);
                ledger.HasIndex(l => new { l.PaymentId, l.CreatedAt });
            });

            // Indexes
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.CreatedAt);

            // Constraints
            builder.HasCheckConstraint("CK_Amount_Positive", "\"Amount\" > 0");
            builder.HasCheckConstraint("CK_Status_Valid", "\"Status\" IN ('Pending','Completed','Failed','Cancelled')");
        }
    }
}
