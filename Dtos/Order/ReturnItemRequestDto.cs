using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Order;

/// <summary>
/// DTO để nhân viên yêu cầu trả món (trước khi thanh toán)
/// </summary>
public class ReturnItemRequestDto
{
    public long OrderDetailId { get; set; }
    public int ReturnQuantity { get; set; }
    public string ReturnReason { get; set; } = string.Empty;

    /// <summary>
    /// true nếu sản phẩm Regular còn nguyên vẹn → cộng lại kho
    /// </summary>
    public bool IsIntact { get; set; } = false;

    /// <summary>
    /// Cách xử lý: Discard, StaffUse, Reuse. 
    /// Nếu null → tự suy từ IsIntact (intact=Reuse, else=Discard)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LeftoverHandlingAction? HandlingAction { get; set; }

    /// <summary>
    /// ID tài khoản bếp làm sai (nếu có)
    /// </summary>
    public long? AtFaultAccountId { get; set; }
}
