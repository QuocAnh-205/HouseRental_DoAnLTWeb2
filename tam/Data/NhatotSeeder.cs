using System.Text.Json;
using HouseRentalAPI.Models;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace HouseRentalAPI.Data;

/// <summary>
/// Nhập tin cho thuê thật ở Đà Nẵng (lấy từ nhatot.com) trong Data/SeedData/nhatot-listings.json.
/// Mỗi người đăng trở thành một tài khoản chủ nhà: nhatot{sellerId}@houserental.vn / Landlord@123.
/// </summary>
public static class NhatotSeeder
{
    private const string EmailDomain = "@houserental.vn";
    private const string EmailPrefix = "nhatot";
    private const string DefaultPassword = "Landlord@123";

    private sealed record Listing(
        long AdId, string Title, string Description, string Address, string? Ward, string District, string City,
        string Type, double Area, int Bedrooms, int Bathrooms, decimal Price, decimal Deposit,
        List<string> Amenities, double? Latitude, double? Longitude, long? ListTime,
        long? SellerId, string SellerName, string? SellerAvatar, List<string> Images);

    public static async Task SeedAsync(AppDbContext context, string contentRoot, ILogger logger)
    {
        var path = Path.Combine(contentRoot, "Data", "SeedData", "nhatot-listings.json");
        if (!File.Exists(path))
            return;
        if (await context.Users.AnyAsync(u => u.Email.StartsWith(EmailPrefix) && u.Email.EndsWith(EmailDomain)))
            return;

        var listings = JsonSerializer.Deserialize<List<Listing>>(
            await File.ReadAllTextAsync(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        if (listings.Count == 0)
            return;

        logger.LogInformation("Nhập {Count} tin từ nhatot.com...", listings.Count);

        // Hash mật khẩu một lần cho tất cả tài khoản mẫu (BCrypt chậm)
        var passwordHash = BC.HashPassword(DefaultPassword);
        var landlords = new Dictionary<long, User>();
        var now = DateTime.UtcNow;

        foreach (var l in listings)
        {
            var sellerId = l.SellerId ?? l.AdId;
            if (!landlords.TryGetValue(sellerId, out var landlord))
            {
                landlord = new User
                {
                    FullName = string.IsNullOrWhiteSpace(l.SellerName) ? "Chủ nhà" : Truncate(l.SellerName.Trim(), 100),
                    Email = $"{EmailPrefix}{sellerId}{EmailDomain}",
                    PasswordHash = passwordHash,
                    // Số điện thoại giả lập (Nhà Tốt không công khai số thật)
                    PhoneNumber = $"09{sellerId % 100_000_000:D8}",
                    AvatarUrl = l.SellerAvatar,
                    Role = UserRole.Landlord,
                    CreatedAt = now.AddDays(-90)
                };
                landlords[sellerId] = landlord;
                context.Users.Add(landlord);
            }

            var createdAt = l.ListTime is > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(l.ListTime.Value).UtcDateTime
                : now;
            if (createdAt > now) createdAt = now;

            context.Properties.Add(new Property
            {
                Title = Truncate(l.Title, 200),
                Description = Truncate(l.Description, 4000),
                Address = Truncate(l.Address, 300),
                Ward = l.Ward is null ? null : Truncate(l.Ward, 100),
                District = Truncate(l.District, 100),
                City = l.City,
                Type = Enum.TryParse<PropertyType>(l.Type, out var type) ? type : PropertyType.Room,
                Area = l.Area > 0 ? l.Area : 20,
                Bedrooms = Math.Clamp(l.Bedrooms, 0, 20),
                Bathrooms = Math.Clamp(l.Bathrooms, 0, 20),
                PricePerMonth = l.Price,
                Deposit = l.Deposit,
                Amenities = l.Amenities,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Status = PropertyStatus.Available,
                ViewCount = (int)(l.AdId % 250),
                Landlord = landlord,
                CreatedAt = createdAt,
                Images = l.Images
                    .Where(url => url.Length <= 500)
                    .Select((url, i) => new PropertyImage { Url = url, IsThumbnail = i == 0, CreatedAt = createdAt })
                    .ToList()
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Đã nhập {Count} tin, {Landlords} chủ nhà từ nhatot.com.", listings.Count, landlords.Count);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
