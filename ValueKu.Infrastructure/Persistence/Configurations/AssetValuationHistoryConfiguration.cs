using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueKu.Core.Entities;

namespace ValueKu.Infrastructure.Persistence.Configurations;

public sealed class AssetValuationHistoryConfiguration : IEntityTypeConfiguration<AssetValuationHistory>
{
    public void Configure(EntityTypeBuilder<AssetValuationHistory> b)
    {
        b.ToTable("AssetValuationHistory");
        b.HasKey(h => h.Id);

        b.Property(h => h.Value).HasPrecision(18, 2);
        b.Property(h => h.RecordedDate).IsRequired();

        // One valuation row per asset per day.
        b.HasIndex(h => new { h.AssetId, h.RecordedDate }).IsUnique();
    }
}
