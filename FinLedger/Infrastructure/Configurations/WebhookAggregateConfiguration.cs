using FinLedger.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinLedger.Infrastructure.Configurations
{
    public sealed class WebhookAggregateConfiguration : IEntityTypeConfiguration<WebhookAggregate>
    {
        public void Configure(EntityTypeBuilder<WebhookAggregate> builder)
        {
            builder.ToTable("WebhookAggregates");

            builder.HasKey(w => w.Id);
            builder.Property(w => w.Id).ValueGeneratedNever();

            builder.Property(w => w.PaymentId).IsRequired();
            builder.Property(w => w.CreatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .ValueGeneratedOnAdd();

            // Owned collection: WebhookDeliveries
            builder.OwnsMany(w => w.Deliveries, delivery =>
            {
                delivery.WithOwner()
                        .HasForeignKey("WebhookAggregateId"); // shadow FK

                delivery.HasKey(d => d.Id);

                delivery.Property(d => d.Id).ValueGeneratedNever();
                delivery.Property(d => d.Url).HasMaxLength(500).IsRequired();
                delivery.Property(d => d.EncryptedPayload).IsRequired();
                delivery.Property(d => d.RetryCount).HasDefaultValue(0);
                delivery.Property(d => d.IsSuccessful).HasDefaultValue(false);
                delivery.Property(d => d.CreatedAt)
                        .HasDefaultValueSql("CURRENT_TIMESTAMP")
                        .ValueGeneratedOnAdd();

                delivery.ToTable("WebhookDeliveries");

                // Indexes
                delivery.HasIndex(d => d.PaymentId);
                delivery.HasIndex(d => d.NextRetryAt);
            });

            // Indexes
            builder.HasIndex(w => w.PaymentId).IsUnique();
            builder.HasIndex(w => w.CreatedAt);
        }
    }
}
