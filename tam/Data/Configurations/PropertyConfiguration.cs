using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HouseRentalAPI.Data.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.Address).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Ward).HasMaxLength(100);
        builder.Property(p => p.District).HasMaxLength(100).IsRequired();
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PricePerMonth).HasPrecision(18, 2);
        builder.Property(p => p.Deposit).HasPrecision(18, 2);
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.RejectReason).HasMaxLength(500);

        builder.HasOne(p => p.Landlord)
            .WithMany(u => u.Properties)
            .HasForeignKey(p => p.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Status);
        // Phục vụ các kiểu sắp xếp phổ biến của trang chủ / tìm kiếm mà không cần sort trong bộ nhớ
        builder.HasIndex(p => new { p.Status, p.CreatedAt });
        builder.HasIndex(p => new { p.Status, p.Type, p.ViewCount });
        builder.HasIndex(p => new { p.Latitude, p.Longitude });
        builder.HasIndex(p => new { p.Status, p.PublishedAt });
        builder.HasIndex(p => new { p.City, p.District });
        builder.HasIndex(p => p.PricePerMonth);
    }
}
