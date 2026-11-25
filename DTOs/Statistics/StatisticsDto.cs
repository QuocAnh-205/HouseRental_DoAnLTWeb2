namespace HouseRentalAPI.DTOs.Statistics;

public record MonthlyRevenueDto(int Year, int Month, decimal Revenue);

public record AdminStatisticsDto(
    int TotalUsers,
    IReadOnlyDictionary<string, int> UsersByRole,
    int LockedUsers,
    int TotalProperties,
    IReadOnlyDictionary<string, int> PropertiesByStatus,
    int TotalBookings,
    IReadOnlyDictionary<string, int> BookingsByStatus,
    int PendingReports,
    decimal TotalRevenue,
    IReadOnlyList<MonthlyRevenueDto> RevenueByMonth);

public record LandlordStatisticsDto(
    int TotalProperties,
    IReadOnlyDictionary<string, int> PropertiesByStatus,
    int TotalViews,
    double AverageRating,
    int TotalReviews,
    int PendingBookings,
    int ActiveContracts,
    int PendingAppointments,
    int UnpaidInvoices,
    decimal OutstandingAmount,
    decimal TotalRevenue,
    IReadOnlyList<MonthlyRevenueDto> RevenueByMonth);

public record TenantStatisticsDto(
    int ActiveContracts,
    int PendingBookings,
    int UpcomingAppointments,
    int Favorites,
    int UnpaidInvoices,
    decimal OutstandingAmount,
    decimal TotalPaid);
