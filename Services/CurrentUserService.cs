using System.Security.Claims;
using HouseRentalAPI.Constants;
using HouseRentalAPI.Exceptions;

namespace HouseRentalAPI.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirst(AppClaimTypes.UserId)?.Value, out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsAdmin => IsInRole(AppRoles.Admin);

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public int GetRequiredUserId() =>
        UserId ?? throw new UnauthorizedException("Bạn chưa đăng nhập");
}
