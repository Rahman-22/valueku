using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValueKu.Core.Entities;

namespace ValueKu.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.Id);

        b.Property(u => u.Username).IsRequired().HasMaxLength(64);
        b.Property(u => u.Email).IsRequired().HasMaxLength(256);
        b.Property(u => u.FirstName).HasMaxLength(64);
        b.Property(u => u.LastName).HasMaxLength(64);
        b.Property(u => u.PhoneCountryCode).HasMaxLength(8);
        b.Property(u => u.PhoneNumber).HasMaxLength(32);
        b.Property(u => u.AvatarUrl).HasMaxLength(256);
        b.Property(u => u.PasswordHash).HasMaxLength(512); // null for external-only (Google) accounts
        b.Property(u => u.GoogleId).HasMaxLength(128);
        b.Property(u => u.CreatedAt).IsRequired();

        b.HasIndex(u => u.Username).IsUnique();
        b.HasIndex(u => u.Email).IsUnique();
        // Unique only among rows that actually have a Google id (multiple NULLs allowed).
        b.HasIndex(u => u.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");

        b.HasMany(u => u.Assets)
            .WithOne(a => a.User!)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(u => u.Accounts)
            .WithOne(a => a.User!)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
