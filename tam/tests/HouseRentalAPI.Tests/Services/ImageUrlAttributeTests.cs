using HouseRentalAPI.DTOs.Common;

namespace HouseRentalAPI.Tests.Services;

/// <summary>Validate URL ảnh: chấp nhận /uploads/... (ảnh upload, dữ liệu mẫu) và http(s)://...</summary>
public class ImageUrlAttributeTests
{
    private readonly ImageUrlAttribute _attr = new();

    [Theory]
    [InlineData("/uploads/2026/11/abc.jpg")]
    [InlineData("http://localhost:5224/uploads/2026/11/abc.jpg")]
    [InlineData("https://images.example.com/a.png")]
    public void UrlHopLe_ChapNhan(string url) => Assert.True(_attr.IsValid(url));

    [Theory]
    [InlineData("")]
    [InlineData("abc.jpg")]
    [InlineData("//evil.com/a.jpg")]
    [InlineData("/uploads/../appsettings.json")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://host/a.jpg")]
    public void UrlKhongHopLe_TuChoi(string url) => Assert.False(_attr.IsValid(url));

    [Fact]
    public void DanhSach_CoMotUrlSai_TuChoi() =>
        Assert.False(_attr.IsValid(new List<string> { "/uploads/a.jpg", "not-a-url" }));

    [Fact]
    public void Null_BoQua_DeRequiredXuLy() => Assert.True(_attr.IsValid(null));
}
