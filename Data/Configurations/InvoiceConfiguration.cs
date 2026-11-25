using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HouseRentalAPI.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.Property(i => i.RentAmount).HasPrecision(18, 2);
        builder.Property(i => i.ElectricityUnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.ElectricityAmount).HasPrecision(18, 2);
        builder.Property(i => i.WaterUnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.WaterAmount).HasPrecision(18, 2);
        builder.Property(i => i.ServiceFee).HasPrecision(18, 2);
        builder.Property(i => i.OtherFee).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Note).HasMaxLength(500);

        builder.HasOne(i => i.Booking)
            .WithMany(b => b.Invoices)
            .HasForeignKey(i => i.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mỗi hợp đồng chỉ có một hóa đơn CÒN HIỆU LỰC cho mỗi tháng.
        // Hóa đơn đã hủy (Cancelled) không tính → chủ nhà hủy hóa đơn lập nhầm rồi lập lại được (khớp InvoiceRepository.ExistsAsync).
        builder.HasIndex(i => new { i.BookingId, i.Year, i.Month })
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled'");
    }
}
