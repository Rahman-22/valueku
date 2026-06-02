using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueKu.Core.Entities;

namespace ValueKu.Infrastructure.Persistence.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> b)
    {
        b.ToTable("Assets");
        b.HasKey(a => a.Id);

        b.Property(a => a.Name).IsRequired().HasMaxLength(128);
        b.Property(a => a.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(a => a.CalculationType).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(a => a.PurchasePrice).HasPrecision(18, 2);
        b.Property(a => a.CurrentValue).HasPrecision(18, 2);
        b.Property(a => a.AppreciationDepreciationRate).HasPrecision(9, 4);
        b.Property(a => a.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("MYR");
        b.Property(a => a.PurchaseDate).IsRequired();

        b.HasMany(a => a.ValuationHistory)
            .WithOne(h => h.Asset!)
            .HasForeignKey(h => h.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(a => a.UserId);
    }
}
