using System.ComponentModel.DataAnnotations;

namespace HouseRentalAPI.DTOs.Reviews;

public record ReviewDto(
    int Id,
    int PropertyId,
    int TenantId,
    string TenantName,
    string? TenantAvatarUrl,
    int Rating,
    string? Comment,
    string? LandlordReply,
    DateTime? RepliedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class CreateReviewDto
{
    [Range(1, 5, ErrorMessage = "Điểm đánh giá từ 1 đến 5")]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public class ReplyReviewDto
{
    [Required(ErrorMessage = "Nội dung phản hồi là bắt buộc")]
    [StringLength(1000)]
    public string Reply { get; set; } = string.Empty;
}

/// <summary>
/// Quyền đánh giá của người đang đăng nhập với một căn nhà: frontend dùng để quyết định có hiện form viết đánh giá không.
/// </summary>
/// <param name="CanReview">Đã từng thuê (hợp đồng Approved/Completed) và chưa đánh giá</param>
/// <param name="HasRented">Đã từng thuê căn này</param>
/// <param name="MyReview">Đánh giá đã viết (nếu có) để hiện nút Sửa</param>
public record ReviewEligibilityDto(bool CanReview, bool HasRented, ReviewDto? MyReview);
