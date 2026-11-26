namespace HouseRentalAPI.Models;

public class Property : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public PropertyType Type { get; set; }
    public double Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public decimal PricePerMonth { get; set; }
    public decimal Deposit { get; set; }
    public List<string> Amenities { get; set; } = [];

    // Tọa độ để tìm nhà quanh một vị trí
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public PropertyStatus Status { get; set; } = PropertyStatus.PendingApproval;
    public string? RejectReason { get; set; }

    /// <summary>Thời điểm tin được duyệt lần đầu và xuất hiện trên Bản tin (null = chưa từng được duyệt)</summary>
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }

    // Lưu sẵn để không phải tính lại mỗi lần hiển thị danh sách
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public int LandlordId { get; set; }
    public User Landlord { get; set; } = null!;

    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
