namespace HouseRentalAPI.Models;

public class PropertyImage : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public bool IsThumbnail { get; set; }

    public int PropertyId { get; set; }
    public Property Property { get; set; } = null!;
}
