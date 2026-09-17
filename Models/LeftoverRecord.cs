using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

// "Món thừa" — ghi nhận 2 nghiệp vụ tách biệt để dễ quản lý:
//   Return: khách trả món (gắn với 1 OrderDetail cụ thể, có thể có hoàn tiền/giảm giá,
//           tồn kho chỉ được cộng lại ở nơi khác nếu còn nguyên vẹn — record này chỉ ghi nhận).
//   Extra:  bếp làm dư/estimate sai (không gắn đơn khách, không đụng tổng tiền order,
//           không cộng trả kho nguyên liệu vì nguyên liệu đã dùng rồi).
[Table("LeftoverRecords")]
public class LeftoverRecord
{
    // Lý do cố định gán tự động khi bếp làm chậm khiến khách không nhận món đã
    // hoàn thành (xem OrderDetailService.CancelOrderDetailAsync). Dùng chung hằng
    // số này để OrderService có thể nhận diện và cộng tích điểm bù cho khách.
    public const string KitchenDelayReason = "Bếp làm chậm, khách không nhận món đã hoàn thành";

    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public LeftoverType Type { get; set; }

    public long ProductId { get; set; }

    public int Quantity { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    // Return-only: chi tiết đơn hàng bị trả
    public long? OrderDetailId { get; set; }
    public LeftoverHandlingAction? HandlingAction { get; set; }

    // Tài khoản bếp (Chef) bị gắn trách nhiệm khi món bị trả do làm sai — phục vụ
    // chọn khi lập biên bản trả món, không bắt buộc chính bếp phải tự khai báo.
    public long? AtFaultAccountId { get; set; }

    // Reuse: trong 30p kể từ CreatedAt, nếu có đơn khác cần đúng món này thì
    // gợi ý dùng lại thay vì bắt bếp làm mới. Set khi được dùng lại.
    public DateTime? UsedAt { get; set; }
    public long? UsedByOrderDetailId { get; set; }

    // Extra-only (nhưng để chung cho cả 2 loại nếu cần tra cứu theo ca/ngày)
    public long? ShiftId { get; set; }
    public DateOnly RecordDate { get; set; }

    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    [ForeignKey("OrderDetailId")]
    public OrderDetail? OrderDetail { get; set; }

    [ForeignKey("UsedByOrderDetailId")]
    public OrderDetail? UsedByOrderDetail { get; set; }

    [ForeignKey("ShiftId")]
    public Shift? Shift { get; set; }

    [ForeignKey("CreatedBy")]
    public Account? Creator { get; set; }

    [ForeignKey("AtFaultAccountId")]
    public Account? AtFaultAccount { get; set; }
}
