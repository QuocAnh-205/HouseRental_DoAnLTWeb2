using HouseRentalAPI.Models;

namespace HouseRentalAPI.Constants;

public static class AppRoles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Landlord = nameof(UserRole.Landlord);
    public const string Tenant = nameof(UserRole.Tenant);

    public const string AdminOrLandlord = Admin + "," + Landlord;
}
