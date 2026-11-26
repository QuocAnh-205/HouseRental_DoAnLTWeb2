namespace HouseRentalAPI.Models;

/// <summary>Nhà được người dùng lưu vào danh sách yêu thích</summary>
public class Favorite : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
}
