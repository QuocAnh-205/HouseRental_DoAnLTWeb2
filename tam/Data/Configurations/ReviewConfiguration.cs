using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HouseRentalAPI.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));

        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.LandlordReply).HasMaxLength(1000);

        builder.HasOne(r => r.Property)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Tenant)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mỗi người thuê chỉ được đánh giá một nhà một lần
        builder.HasIndex(r => new { r.PropertyId, r.TenantId }).IsUnique();
    }
}
