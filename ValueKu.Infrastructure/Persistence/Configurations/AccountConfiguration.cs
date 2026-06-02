using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueKu.Core.Entities;

namespace ValueKu.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("Accounts");
        b.HasKey(a => a.Id);

        b.Property(a => a.Name).IsRequired().HasMaxLength(128);
        b.Property(a => a.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(a => a.Balance).HasPrecision(18, 2);
        b.Property(a => a.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("MYR");

        b.HasMany(a => a.Transactions)
            .WithOne(t => t.Account!)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(a => a.UserId);
    }
}
