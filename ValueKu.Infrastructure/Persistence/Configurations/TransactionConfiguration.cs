using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueKu.Core.Entities;

namespace ValueKu.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("Transactions");
        b.HasKey(t => t.Id);

        b.Property(t => t.Amount).HasPrecision(18, 2);
        b.Property(t => t.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(t => t.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(t => t.Description).HasMaxLength(256);
        b.Property(t => t.TransactionDate).IsRequired();

        b.HasIndex(t => t.AccountId);
        b.HasIndex(t => t.TransactionDate);
    }
}
