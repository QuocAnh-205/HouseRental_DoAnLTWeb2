namespace HouseRentalAPI.Constants;

/// <summary>
/// Thời gian nghiệp vụ theo giờ Việt Nam (UTC+7).
/// CSDL vẫn lưu DateTime ở UTC; chỉ dùng helper này để:
///  - lấy "hôm nay" theo lịch Việt Nam (tránh lệch 1 ngày khi test trong khung 00:00–06:59 sáng),
///  - hiển thị giờ trong nội dung thông báo cho người dùng.
/// </summary>
public static class AppTime
{
    public static readonly TimeZoneInfo VietnamZone = ResolveZone();

    /// <summary>Ngày hiện tại theo giờ Việt Nam</summary>
    public static DateOnly Today => DateOnly.FromDateTime(ToVietnam(DateTime.UtcNow));

    /// <summary>Đổi một thời điểm UTC sang giờ Việt Nam</summary>
    public static DateTime ToVietnam(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), VietnamZone);

    /// <summary>Chuỗi hiển thị cho thông báo, ví dụ "09:00 25/11/2026"</summary>
    public static string Format(DateTime utc) => ToVietnam(utc).ToString("HH:mm dd/MM/yyyy");

    private static TimeZoneInfo ResolveZone()
    {
        // Windows: "SE Asia Standard Time"; Linux/macOS: "Asia/Ho_Chi_Minh" (.NET 6+ tự chuyển đổi qua ICU)
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("UTC+07", TimeSpan.FromHours(7), "Giờ Việt Nam", "Giờ Việt Nam");
    }
}
