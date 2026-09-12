using MenuGoBE.Models.Enums;
using MenuGoBE.Dtos.Product;

namespace MenuGoBE.Dtos.Promotion;

public class PromotionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal MaxDiscount { get; set; }
    public PromotionScope Scope { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty; // Upcoming, Active, Expired
    public bool HasBeenUsed { get; set; }
    
    // Detailed info
    public List<PromotionProductDto> Products { get; set; } = new List<PromotionProductDto>();
    public List<PromotionBranchDto> Branches { get; set; } = new List<PromotionBranchDto>();
}

public class PromotionProductDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
}

public class PromotionBranchDto
{
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
}
