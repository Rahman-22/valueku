using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueKu.Core.Entities;

namespace ValueKu.Infrastructure.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> b)
    {
        b.ToTable("Budgets");
        b.HasKey(x => x.Id);

        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.MonthlyLimit).HasPrecision(18, 2);
        b.Property(x => x.CreatedAt).IsRequired();

        // One budget per category per user.
        b.HasIndex(x => new { x.UserId, x.Category }).IsUnique();

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
