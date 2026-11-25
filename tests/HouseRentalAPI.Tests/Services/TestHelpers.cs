using HouseRentalAPI.Constants;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Services;
using Moq;

namespace HouseRentalAPI.Tests.Services;

internal static class TestHelpers
{
    /// <summary>Giả lập người dùng đang đăng nhập với vai trò cho trước</summary>
    public static Mock<ICurrentUserService> CurrentUser(int? userId, string role = AppRoles.Tenant)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        mock.SetupGet(x => x.IsAuthenticated).Returns(userId.HasValue);
        mock.SetupGet(x => x.IsAdmin).Returns(role == AppRoles.Admin);
        mock.Setup(x => x.IsInRole(It.IsAny<string>())).Returns<string>(r => r == role);
        mock.Setup(x => x.GetRequiredUserId()).Returns(() => userId ?? throw new UnauthorizedException("Bạn chưa đăng nhập"));
        return mock;
    }

    public static DateOnly Today => AppTime.Today;
}
