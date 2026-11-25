using HouseRentalAPI.Constants;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace HouseRentalAPI.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));

        // Có migration thì apply migration, chưa có thì tạo DB trực tiếp từ model
        if (context.Database.GetMigrations().Any())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();

        if (!await context.Users.AnyAsync())
            await SeedSampleAsync(context, logger);

        // Tin cho thuê thật ở Đà Nẵng lấy từ nhatot.com (chỉ nhập một lần)
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        await NhatotSeeder.SeedAsync(context, env.ContentRootPath, logger);

        // Dữ liệu demo: chat với admin, tin chờ duyệt, hợp đồng nhiều trạng thái (chỉ chạy một lần)
        await DemoSeeder.SeedAsync(context, logger);

        // Tin mẫu đã được duyệt sẵn: gán thời điểm xuất hiện trên Bản tin = ngày đăng
        await context.Properties
            .Where(p => p.PublishedAt == null &&
                        (p.Status == PropertyStatus.Available || p.Status == PropertyStatus.Rented || p.Status == PropertyStatus.Hidden))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PublishedAt, p => p.CreatedAt));
    }

    private static async Task SeedSampleAsync(AppDbContext context, ILogger logger)
    {

        logger.LogInformation("Seeding sample data...");

        var today = AppTime.Today;
        var now = DateTime.UtcNow;

        // ===== Người dùng =====
        var admin = new User
        {
            FullName = "Quản trị viên",
            Email = "admin@houserental.vn",
            PasswordHash = BC.HashPassword("Admin@123"),
            PhoneNumber = "0900000001",
            Role = UserRole.Admin
        };
        var landlord = new User
        {
            FullName = "Nguyễn Văn Chủ",
            Email = "landlord@houserental.vn",
            PasswordHash = BC.HashPassword("Landlord@123"),
            PhoneNumber = "0900000002",
            Role = UserRole.Landlord
        };
        var tenant = new User
        {
            FullName = "Trần Thị Thuê",
            Email = "tenant@houserental.vn",
            PasswordHash = BC.HashPassword("Tenant@123"),
            PhoneNumber = "0900000003",
            Role = UserRole.Tenant
        };

        context.Users.AddRange(admin, landlord, tenant);

        // ===== Nhà cho thuê =====
        var apartment = new Property
        {
            Title = "Căn hộ 2PN view sông Hàn",
            Description = "Căn hộ đầy đủ nội thất, gần cầu Rồng, an ninh 24/7.",
            Address = "123 Bạch Đằng",
            Ward = "Hải Châu 1",
            District = "Hải Châu",
            City = "Đà Nẵng",
            Latitude = 16.0678,
            Longitude = 108.2240,
            Type = PropertyType.Apartment,
            Area = 75,
            Bedrooms = 2,
            Bathrooms = 2,
            PricePerMonth = 9_000_000,
            Deposit = 9_000_000,
            Amenities = ["Wifi", "Điều hòa", "Máy giặt", "Thang máy", "Bãi đỗ xe"],
            Status = PropertyStatus.Rented,
            ViewCount = 128,
            Landlord = landlord,
            Images =
            [
                new PropertyImage { Url = "https://picsum.photos/seed/apartment1/800/600", IsThumbnail = true },
                new PropertyImage { Url = "https://picsum.photos/seed/apartment2/800/600" }
            ]
        };
        var room = new Property
        {
            Title = "Phòng trọ sinh viên gần ĐH Đông Á",
            Description = "Phòng mới xây, có gác lửng, giờ giấc tự do.",
            Address = "33 Xô Viết Nghệ Tĩnh",
            Ward = "Hòa Cường Nam",
            District = "Hải Châu",
            City = "Đà Nẵng",
            Latitude = 16.0322,
            Longitude = 108.2210,
            Type = PropertyType.Room,
            Area = 22,
            Bedrooms = 1,
            Bathrooms = 1,
            PricePerMonth = 2_500_000,
            Deposit = 2_500_000,
            Amenities = ["Wifi", "Gác lửng", "Nóng lạnh"],
            Status = PropertyStatus.Available,
            ViewCount = 56,
            AverageRating = 5,
            ReviewCount = 1,
            Landlord = landlord,
            Images = [new PropertyImage { Url = "https://picsum.photos/seed/room1/800/600", IsThumbnail = true }]
        };
        var house = new Property
        {
            Title = "Nhà nguyên căn 3 tầng gần biển Mỹ Khê",
            Description = "Nhà mặt tiền, phù hợp gia đình hoặc kinh doanh homestay.",
            Address = "45 Hồ Nghinh",
            Ward = "Phước Mỹ",
            District = "Sơn Trà",
            City = "Đà Nẵng",
            Latitude = 16.0614,
            Longitude = 108.2468,
            Type = PropertyType.House,
            Area = 120,
            Bedrooms = 4,
            Bathrooms = 3,
            PricePerMonth = 20_000_000,
            Deposit = 40_000_000,
            Amenities = ["Wifi", "Điều hòa", "Sân thượng", "Bếp", "Gara ô tô"],
            Status = PropertyStatus.PendingApproval,
            Landlord = landlord
        };
        var studio = new Property
        {
            Title = "Studio mini full nội thất gần biển",
            Description = "Studio 30m², ban công thoáng, cách biển Mỹ Khê 300m.",
            Address = "12 An Thượng 2",
            Ward = "Mỹ An",
            District = "Ngũ Hành Sơn",
            City = "Đà Nẵng",
            Latitude = 16.0505,
            Longitude = 108.2452,
            Type = PropertyType.Apartment,
            Area = 30,
            Bedrooms = 1,
            Bathrooms = 1,
            PricePerMonth = 7_000_000,
            Deposit = 7_000_000,
            Amenities = ["Wifi", "Điều hòa", "Máy giặt", "Ban công"],
            Status = PropertyStatus.Available,
            ViewCount = 34,
            Landlord = landlord,
            Images = [new PropertyImage { Url = "https://picsum.photos/seed/studio1/800/600", IsThumbnail = true }]
        };

        context.Properties.AddRange(apartment, room, house, studio);

        // ===== Hợp đồng đã kết thúc + đánh giá có phản hồi =====
        context.Bookings.Add(new Booking
        {
            Property = room,
            Tenant = tenant,
            StartDate = today.AddMonths(-8),
            EndDate = today.AddMonths(-2),
            DurationMonths = 6,
            MonthlyPrice = room.PricePerMonth,
            Deposit = room.Deposit,
            TotalAmount = room.PricePerMonth * 6,
            Status = BookingStatus.Completed
        });
        context.Reviews.Add(new Review
        {
            Property = room,
            Tenant = tenant,
            Rating = 5,
            Comment = "Phòng sạch, chủ nhà thân thiện, giờ giấc thoải mái.",
            LandlordReply = "Cảm ơn bạn đã ở và đánh giá!",
            RepliedAt = now.AddMonths(-2)
        });

        // ===== Hợp đồng đang hiệu lực: đã cọc, có hóa đơn tháng trước chưa trả =====
        var firstOfThisMonth = today.AddDays(1 - today.Day);
        var activeBooking = new Booking
        {
            Property = apartment,
            Tenant = tenant,
            StartDate = firstOfThisMonth.AddMonths(-1),
            EndDate = firstOfThisMonth.AddMonths(11),
            DurationMonths = 12,
            MonthlyPrice = apartment.PricePerMonth,
            Deposit = apartment.Deposit,
            TotalAmount = apartment.PricePerMonth * 12,
            Status = BookingStatus.Approved,
            Note = "Gia đình 2 người, không nuôi thú cưng"
        };
        context.Bookings.Add(activeBooking);

        context.Payments.Add(new Payment
        {
            Booking = activeBooking,
            Amount = activeBooking.Deposit,
            Type = PaymentType.Deposit,
            Method = PaymentMethod.BankTransfer,
            Status = PaymentStatus.Success,
            TransactionCode = "PAYSEEDDEPOSIT01",
            PaidAt = now.AddMonths(-1)
        });

        var lastMonth = firstOfThisMonth.AddMonths(-1);
        var invoice = new Invoice
        {
            Booking = activeBooking,
            Month = lastMonth.Month,
            Year = lastMonth.Year,
            RentAmount = activeBooking.MonthlyPrice,
            ElectricityOldIndex = 1200,
            ElectricityNewIndex = 1350,
            ElectricityUnitPrice = 3_500,
            ElectricityAmount = 150 * 3_500,
            WaterOldIndex = 300,
            WaterNewIndex = 312,
            WaterUnitPrice = 15_000,
            WaterAmount = 12 * 15_000,
            ServiceFee = 200_000,
            DueDate = today.AddDays(5),
            Status = InvoiceStatus.Unpaid
        };
        invoice.TotalAmount = invoice.RentAmount + invoice.ElectricityAmount + invoice.WaterAmount + invoice.ServiceFee;
        context.Invoices.Add(invoice);

        // ===== Lịch hẹn, yêu thích, tin nhắn, thông báo =====
        context.Appointments.Add(new Appointment
        {
            Property = studio,
            Tenant = tenant,
            ScheduledAt = now.Date.AddDays(2).AddHours(3),
            Note = "Mình muốn xem vào buổi sáng"
        });

        context.Favorites.AddRange(
            new Favorite { User = tenant, Property = studio },
            new Favorite { User = tenant, Property = room });

        var conversation = new Conversation
        {
            Initiator = tenant,
            Recipient = landlord,
            Property = studio,
            LastMessageAt = now.AddMinutes(-5)
        };
        conversation.Messages.Add(new Message { Sender = tenant, Content = "Chào anh, studio còn trống không ạ?", IsRead = true, ReadAt = now.AddMinutes(-8) });
        conversation.Messages.Add(new Message { Sender = landlord, Content = "Còn bạn nhé, bạn muốn xem lúc nào?" });
        context.Conversations.Add(conversation);

        context.Notifications.AddRange(
            new Notification
            {
                User = tenant,
                Type = NotificationType.Invoice,
                Title = $"Hóa đơn tháng {invoice.Month}/{invoice.Year}",
                Message = $"Hóa đơn \"{apartment.Title}\": {invoice.TotalAmount:N0}đ, hạn {invoice.DueDate:dd/MM/yyyy}.",
                Link = "/api/invoices/mine"
            },
            new Notification
            {
                User = landlord,
                Type = NotificationType.Appointment,
                Title = "Có lịch hẹn xem nhà mới",
                Message = $"{tenant.FullName} muốn xem \"{studio.Title}\".",
                Link = "/api/appointments/received"
            },
            new Notification
            {
                User = admin,
                Type = NotificationType.Property,
                Title = "Tin đăng mới chờ duyệt",
                Message = $"Tin \"{house.Title}\" đang chờ duyệt.",
                Link = "/api/properties/manage?status=PendingApproval"
            });

        await context.SaveChangesAsync();
        logger.LogInformation("Seed data completed.");
    }
}
