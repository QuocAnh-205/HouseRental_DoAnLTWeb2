using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HouseRentalAPI.Data.Configurations;

public class UploadedImageConfiguration : IEntityTypeConfiguration<UploadedImage>
{
    public void Configure(EntityTypeBuilder<UploadedImage> builder)
    {
        builder.ToTable("UploadedImages");

        builder.Property(i => i.Url).HasMaxLength(500).IsRequired();
        builder.Property(i => i.FilePath).HasMaxLength(300).IsRequired();
        builder.Property(i => i.FileName).HasMaxLength(100).IsRequired();
        builder.Property(i => i.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(i => i.ContentType).HasMaxLength(100).IsRequired();

        builder.HasIndex(i => i.Url).IsUnique();
        builder.HasIndex(i => new { i.UploadedById, i.CreatedAt });

        builder.HasOne(i => i.UploadedBy)
            .WithMany()
            .HasForeignKey(i => i.UploadedById)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
