using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace HouseRentalAPI.Data;

/// <summary>
/// Dữ liệu demo bổ sung để chấm bài / trình diễn (chỉ chạy một lần, nhận biết qua tài khoản tenant2):
/// - Thêm 1 người thuê và 1 chủ nhà mẫu
/// - Các cuộc trò chuyện giữa người dùng mẫu và quản trị viên (hỗ trợ)
/// - Các tin đăng đang chờ admin duyệt
/// - Nhiều hợp đồng ở đủ trạng thái: chờ duyệt, đang hiệu lực, đã kết thúc, bị từ chối, đã hủy
/// </summary>
public static class DemoSeeder
{
    public const string Tenant2Email = "tenant2@houserental.vn";
    public const string Landlord2Email = "landlord2@houserental.vn";

    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Users.AnyAsync(u => u.Email == Tenant2Email))
            return;

        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@houserental.vn");
        var landlord = await context.Users.FirstOrDefaultAsync(u => u.Email == "landlord@houserental.vn");
        var tenant = await context.Users.FirstOrDefaultAsync(u => u.Email == "tenant@houserental.vn");
        if (admin is null || landlord is null || tenant is null)
            return;

        logger.LogInformation("Seeding demo data (support chats, pending listings, contracts)...");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var firstOfThisMonth = today.AddDays(1 - today.Day);

        // Ảnh thật lấy từ các tin đã nhập để tin demo trông giống thật
        var photos = await context.PropertyImages
            .Where(i => i.Url.StartsWith("/uploads/nhatot/"))
            .OrderBy(i => i.Id)
            .Select(i => i.Url)
            .Take(60)
            .ToListAsync();
        string Photo(int i) => photos.Count > 0 ? photos[i % photos.Count] : $"https://picsum.photos/seed/demo{i}/800/600";
        List<PropertyImage> Images(int start, int count) =>
            Enumerable.Range(start, count).Select((n, idx) => new PropertyImage { Url = Photo(n * 3), IsThumbnail = idx == 0 }).ToList();

        // ===== Người dùng mẫu =====
        var tenant2 = new User
        {
            FullName = "Lê Minh Khách",
            Email = Tenant2Email,
            PasswordHash = BC.HashPassword("Tenant@123"),
            PhoneNumber = "0900000004",
            Role = UserRole.Tenant,
            CreatedAt = now.AddMonths(-10)
        };
        var landlord2 = new User
        {
            FullName = "Phạm Thị Nhà",
            Email = Landlord2Email,
            PasswordHash = BC.HashPassword("Landlord@123"),
            PhoneNumber = "0900000005",
            Role = UserRole.Landlord,
            CreatedAt = now.AddMonths(-6)
        };
        context.Users.AddRange(tenant2, landlord2);

        // ===== Tin đăng của chủ nhà mẫu (dùng cho hợp đồng) =====
        Property NewProperty(User owner, string title, string description, string address, string ward, string district,
            double lat, double lng, PropertyType type, double area, int bedrooms, decimal price, PropertyStatus status,
            string[] amenities, int photoStart, int daysAgo) => new()
        {
            Landlord = owner,
            Title = title,
            Description = description,
            Address = address,
            Ward = ward,
            District = district,
            City = "Đà Nẵng",
            Latitude = lat,
            Longitude = lng,
            Type = type,
            Area = area,
            Bedrooms = bedrooms,
            Bathrooms = 1,
            PricePerMonth = price,
            Deposit = price,
            Amenities = [.. amenities],
            Status = status,
            ViewCount = status == PropertyStatus.PendingApproval ? 0 : 40 + photoStart * 7,
            Images = Images(photoStart, 3),
            CreatedAt = now.AddDays(-daysAgo)
        };

        var bachKhoa = NewProperty(landlord, "Phòng trọ mới xây gần ĐH Bách Khoa",
            "Phòng 25m² có gác lửng, cửa sổ thoáng, camera an ninh, giờ giấc tự do. Đi bộ 5 phút tới ĐH Bách Khoa.",
            "54 Nguyễn Lương Bằng", "Hòa Khánh Bắc", "Liên Chiểu", 16.0738, 108.1500,
            PropertyType.Room, 25, 1, 2_800_000, PropertyStatus.Rented, ["Wifi", "Gác lửng", "Nóng lạnh", "Chỗ để xe"], 1, 120);
        var sonTra = NewProperty(landlord, "Căn hộ 1PN view biển Sơn Trà",
            "Căn hộ 45m² đầy đủ nội thất, ban công nhìn ra biển Phạm Văn Đồng, có hồ bơi chung.",
            "88 Võ Nguyên Giáp", "Phước Mỹ", "Sơn Trà", 16.0702, 108.2455,
            PropertyType.Apartment, 45, 1, 8_500_000, PropertyStatus.Available, ["Wifi", "Điều hòa", "Máy giặt", "Hồ bơi", "Thang máy"], 2, 60);
        var camLe = NewProperty(landlord2, "Nhà nguyên căn 2 tầng khu Hòa Xuân",
            "Nhà 2 tầng, 3 phòng ngủ, sân để ô tô, khu dân cư yên tĩnh, gần cầu Hòa Xuân.",
            "27 Lê Quang Hòa", "Hòa Xuân", "Cẩm Lệ", 16.0012, 108.2190,
            PropertyType.House, 100, 3, 9_000_000, PropertyStatus.Available, ["Wifi", "Điều hòa", "Bếp", "Gara ô tô"], 3, 90);
        context.Properties.AddRange(bachKhoa, sonTra, camLe);

        // ===== Tin đăng chờ admin duyệt =====
        var pending = new[]
        {
            NewProperty(landlord, "Phòng trọ có ban công gần chợ Hàn",
                "Phòng 20m², ban công riêng, có máy lạnh, cách chợ Hàn 300m. Phù hợp người đi làm.",
                "15 Trần Phú", "Hải Châu 1", "Hải Châu", 16.0711, 108.2238,
                PropertyType.Room, 20, 1, 3_200_000, PropertyStatus.PendingApproval, ["Wifi", "Điều hòa", "Ban công"], 4, 1),
            NewProperty(landlord, "Căn hộ studio full nội thất gần cầu Rồng",
                "Studio 35m², nội thất mới 100%, bếp riêng, máy giặt riêng, có dọn phòng hàng tuần.",
                "210 Trần Hưng Đạo", "An Hải Tây", "Sơn Trà", 16.0613, 108.2312,
                PropertyType.Apartment, 35, 1, 6_000_000, PropertyStatus.PendingApproval, ["Wifi", "Điều hòa", "Máy giặt", "Bếp"], 5, 0),
            NewProperty(landlord2, "Phòng trọ sinh viên giá rẻ gần ĐH Kinh tế",
                "Phòng 18m², có gác, wifi miễn phí, điện nước giá nhà nước. Ưu tiên sinh viên.",
                "71 Ngũ Hành Sơn", "Bắc Mỹ An", "Ngũ Hành Sơn", 16.0478, 108.2401,
                PropertyType.Room, 18, 1, 1_800_000, PropertyStatus.PendingApproval, ["Wifi", "Gác lửng"], 6, 0),
            NewProperty(landlord2, "Văn phòng 60m² mặt tiền Nguyễn Văn Linh",
                "Mặt bằng tầng 2, phù hợp văn phòng công ty nhỏ, có thang máy và chỗ để xe rộng.",
                "300 Nguyễn Văn Linh", "Thạc Gián", "Thanh Khê", 16.0603, 108.2069,
                PropertyType.Office, 60, 0, 12_000_000, PropertyStatus.PendingApproval, ["Thang máy", "Bãi đỗ xe", "Điều hòa"], 7, 0)
        };
        context.Properties.AddRange(pending);

        // ===== Hợp đồng ở nhiều trạng thái =====
        var studio = await context.Properties.FirstOrDefaultAsync(p => p.LandlordId == landlord.Id && p.Title.StartsWith("Studio mini"));
        var room = await context.Properties.FirstOrDefaultAsync(p => p.LandlordId == landlord.Id && p.Title.StartsWith("Phòng trọ sinh viên gần ĐH Đông Á"));

        Booking NewBooking(Property property, User renter, DateOnly start, int months, BookingStatus status, string? note = null, string? reject = null, int createdDaysAgo = 1) => new()
        {
            Property = property,
            Tenant = renter,
            StartDate = start,
            EndDate = start.AddMonths(months),
            DurationMonths = months,
            MonthlyPrice = property.PricePerMonth,
            Deposit = property.Deposit,
            TotalAmount = property.PricePerMonth * months,
            Status = status,
            Note = note,
            RejectReason = reject,
            CreatedAt = now.AddDays(-createdDaysAgo)
        };

        // 1. Hợp đồng đang hiệu lực: tenant2 thuê phòng gần Bách Khoa, đã cọc, có 2 hóa đơn
        var active = NewBooking(bachKhoa, tenant2, firstOfThisMonth.AddMonths(-2), 12, BookingStatus.Approved,
            "Mình là sinh viên năm 3, ở một mình", createdDaysAgo: 70);
        context.Bookings.Add(active);
        context.Payments.Add(new Payment
        {
            Booking = active, Amount = active.Deposit, Type = PaymentType.Deposit, Method = PaymentMethod.Momo,
            Status = PaymentStatus.Success, TransactionCode = "PAYDEMODEPOSIT02", PaidAt = now.AddDays(-65)
        });

        Invoice NewInvoice(DateOnly period, int elecOld, int elecNew, int waterOld, int waterNew, decimal paid, InvoiceStatus status) =>
            new()
            {
                Booking = active, Month = period.Month, Year = period.Year, RentAmount = active.MonthlyPrice,
                ElectricityOldIndex = elecOld, ElectricityNewIndex = elecNew, ElectricityUnitPrice = 3_500,
                ElectricityAmount = (elecNew - elecOld) * 3_500m,
                WaterOldIndex = waterOld, WaterNewIndex = waterNew, WaterUnitPrice = 15_000,
                WaterAmount = (waterNew - waterOld) * 15_000m,
                ServiceFee = 100_000, PaidAmount = paid, Status = status,
                DueDate = period.AddMonths(1).AddDays(9)
            };

        var paidInvoice = NewInvoice(firstOfThisMonth.AddMonths(-2), 500, 590, 100, 106, 0, InvoiceStatus.Paid);
        paidInvoice.TotalAmount = paidInvoice.RentAmount + paidInvoice.ElectricityAmount + paidInvoice.WaterAmount + paidInvoice.ServiceFee;
        paidInvoice.PaidAmount = paidInvoice.TotalAmount;
        var openInvoice = NewInvoice(firstOfThisMonth.AddMonths(-1), 590, 702, 106, 113, 1_000_000, InvoiceStatus.PartiallyPaid);
        openInvoice.TotalAmount = openInvoice.RentAmount + openInvoice.ElectricityAmount + openInvoice.WaterAmount + openInvoice.ServiceFee;
        context.Invoices.AddRange(paidInvoice, openInvoice);
        context.Payments.AddRange(
            new Payment
            {
                Booking = active, Invoice = paidInvoice, Amount = paidInvoice.TotalAmount, Type = PaymentType.MonthlyRent,
                Method = PaymentMethod.BankTransfer, Status = PaymentStatus.Success, TransactionCode = "PAYDEMORENT0201", PaidAt = now.AddDays(-30)
            },
            new Payment
            {
                Booking = active, Invoice = openInvoice, Amount = 1_000_000, Type = PaymentType.MonthlyRent,
                Method = PaymentMethod.BankTransfer, Status = PaymentStatus.Success, TransactionCode = "PAYDEMORENT0202", PaidAt = now.AddDays(-3)
            },
            new Payment
            {
                Booking = active, Invoice = openInvoice, Amount = openInvoice.TotalAmount - 1_000_000, Type = PaymentType.MonthlyRent,
                Method = PaymentMethod.Momo, Status = PaymentStatus.Pending, TransactionCode = "PAYDEMORENT0203", Note = "Em chuyển nốt phần còn lại ạ"
            });

        // 2. Yêu cầu chờ duyệt: 2 người cùng muốn thuê căn hộ Sơn Trà (duyệt 1 thì người kia tự bị từ chối)
        context.Bookings.AddRange(
            NewBooking(sonTra, tenant, today.AddDays(10), 6, BookingStatus.Pending, "Gia đình 2 người, đi làm giờ hành chính"),
            NewBooking(sonTra, tenant2, today.AddDays(14), 12, BookingStatus.Pending, "Mình muốn thuê dài hạn 1 năm", createdDaysAgo: 0));

        // 3. Yêu cầu chờ duyệt với nhà của chủ nhà thứ hai
        context.Bookings.Add(NewBooking(camLe, tenant, today.AddDays(20), 12, BookingStatus.Pending, "Gia đình 4 người, có ô tô", createdDaysAgo: 2));

        // 4. Bị từ chối, đã hủy, đã kết thúc
        if (studio is not null)
            context.Bookings.Add(NewBooking(studio, tenant2, today.AddDays(-20), 3, BookingStatus.Rejected,
                reject: "Chủ nhà chỉ nhận hợp đồng từ 6 tháng", createdDaysAgo: 25));
        context.Bookings.Add(NewBooking(camLe, tenant2, today.AddDays(-40), 6, BookingStatus.Cancelled,
            "Mình đổi kế hoạch nên hủy", createdDaysAgo: 45));
        if (room is not null)
            context.Bookings.Add(NewBooking(room, tenant2, today.AddMonths(-14), 6, BookingStatus.Completed, createdDaysAgo: 430));

        // ===== Trò chuyện với quản trị viên (hỗ trợ) =====
        Conversation SupportChat(User user, params (User Sender, string Content, int MinutesAgo, bool Read)[] messages)
        {
            var conversation = new Conversation { Initiator = user, Recipient = admin, LastMessageAt = now.AddMinutes(-messages[^1].MinutesAgo) };
            foreach (var m in messages)
            {
                conversation.Messages.Add(new Message
                {
                    Sender = m.Sender, Content = m.Content, IsRead = m.Read,
                    ReadAt = m.Read ? now.AddMinutes(-m.MinutesAgo + 1) : null, CreatedAt = now.AddMinutes(-m.MinutesAgo)
                });
            }
            return conversation;
        }

        context.Conversations.AddRange(
            SupportChat(tenant,
                (tenant, "Chào admin, mình muốn hỏi cách thanh toán hóa đơn tiền điện nước ạ?", 180, true),
                (admin, "Chào bạn! Bạn vào mục Hóa đơn, chọn hóa đơn cần trả rồi bấm \"Thanh toán\". Sau khi chủ nhà xác nhận, hóa đơn sẽ chuyển sang Đã thanh toán.", 175, true),
                (tenant, "Dạ cảm ơn admin. Nếu chủ nhà chưa xác nhận thì mình phải làm sao ạ?", 30, false)),
            SupportChat(landlord,
                (landlord, "Admin ơi, tin \"Phòng trọ có ban công gần chợ Hàn\" của tôi bao lâu thì được duyệt?", 90, true),
                (admin, "Chào anh, tin sẽ được duyệt trong vòng 24 giờ. Anh nhớ đăng đủ ảnh thật để tin được duyệt nhanh hơn nhé.", 85, true),
                (landlord, "Tôi vừa đăng thêm tin studio gần cầu Rồng, nhờ admin duyệt giúp luôn nhé.", 12, false)),
            SupportChat(tenant2,
                (tenant2, "Chào admin, em muốn báo một tin đăng có ảnh không đúng thực tế.", 1440, true),
                (admin, "Cảm ơn bạn! Bạn bấm \"Báo cáo tin\" ở cuối trang chi tiết, admin sẽ kiểm tra và ẩn tin nếu vi phạm.", 1430, true)));

        // ===== Thông báo =====
        var pendingNotices = pending.Select(p => (Property: p, Notice: new Notification
        {
            User = admin, Type = NotificationType.Property, Title = "Tin đăng mới chờ duyệt",
            Message = $"Tin \"{p.Title}\" vừa được đăng và đang chờ duyệt.", CreatedAt = p.CreatedAt,
            Link = "/api/properties/manage?status=PendingApproval"
        })).ToList();
        context.Notifications.AddRange(pendingNotices.Select(x => x.Notice));
        context.Notifications.AddRange(
            new Notification
            {
                User = landlord, Type = NotificationType.Booking, Title = "Có yêu cầu thuê mới",
                Message = $"Nhà \"{sonTra.Title}\" nhận được 2 yêu cầu thuê đang chờ bạn duyệt.",
                Link = "/api/bookings/received"
            },
            new Notification
            {
                User = landlord, Type = NotificationType.Payment, Title = "Có thanh toán chờ xác nhận",
                Message = $"{tenant2.FullName} vừa thanh toán phần còn lại hóa đơn tháng {openInvoice.Month}/{openInvoice.Year} cho \"{bachKhoa.Title}\".",
                Link = "/api/payments/received"
            },
            new Notification
            {
                User = tenant2, Type = NotificationType.Invoice, Title = $"Hóa đơn tháng {openInvoice.Month}/{openInvoice.Year}",
                Message = $"Hóa đơn \"{bachKhoa.Title}\": {openInvoice.TotalAmount:N0}đ, đã trả 1.000.000đ.",
                Link = "/api/invoices/mine"
            });

        await context.SaveChangesAsync();

        // Đã có Id: trỏ mỗi thông báo "chờ duyệt" tới đúng tin của nó
        foreach (var (property, notice) in pendingNotices)
            notice.Link = $"/api/properties/{property.Id}";
        await context.SaveChangesAsync();

        logger.LogInformation("Demo data completed.");
    }
}
