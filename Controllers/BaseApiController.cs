using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Controllers;

#region Lớp Controller cơ sở (BaseApiController)
/// <summary>
/// Lớp Controller cơ sở hỗ trợ trích xuất thông tin người dùng từ JWT Access Token.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    #region Hàm Helper Trích Xuất Creator User ID từ Access Token
    /// <summary>
    /// Trích xuất ID người dùng (Creator/User ID) từ Token Access trong Context HTTP.
    /// Nếu trích xuất thất bại hoặc token không hợp lệ, trả về false.
    /// </summary>
    /// <param name="userId">Out variable chứa ID người dùng</param>
    /// <returns>True nếu thành công, False nếu thất bại</returns>
    protected bool TryGetUserId(out long userId)
    {
        userId = 0;
        if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return false;
        }

        // Tìm kiếm các Claim chứa thông tin User ID
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) 
                    ?? User.FindFirst(JwtRegisteredClaimNames.Sub) 
                    ?? User.FindFirst("sub") 
                    ?? User.FindFirst("id")
                    ?? User.FindFirst("AccountId");

        if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
        {
            return false;
        }

        return long.TryParse(claim.Value, out userId) && userId > 0;
    }

    /// <summary>
    /// Hàm hỗ trợ trích xuất User ID và tự động trả về lỗi 401 Unauthorized nếu trích xuất thất bại.
    /// </summary>
    /// <param name="userId">Out variable chứa User ID nếu thành công</param>
    /// <param name="errorResult">Out variable chứa IActionResult 401 nếu thất bại</param>
    /// <returns>True nếu hợp lệ, False nếu từ chối request</returns>
    protected bool ValidateUserToken(out long userId, out IActionResult? errorResult)
    {
        if (!TryGetUserId(out userId))
        {
            errorResult = Unauthorized(new { 
                success = false, 
                message = "Truy cập bị từ chối: Không thể trích xuất ID người dùng từ Access Token. Token không hợp lệ hoặc đã hết hạn." 
            });
            return false;
        }

        errorResult = null;
        return true;
    }

    /// <summary>
    /// Kiểm tra quyền truy cập Chi nhánh dựa trên Token Access và targetBranchId truyền từ Request.
    /// - Vai trò Admin / Owner: Cho phép thao tác lập tức với kho dựa trên branchid truyền vào từ bên ngoài.
    /// - Các vai trò khác (Manager, Cashier, Chef, Waiter): Bắt buộc targetBranchId phải nằm trong danh sách branchId của Token.
    /// </summary>
    /// <param name="targetBranchId">ID Chi nhánh cần thao tác (truyền từ Query hoặc Document body)</param>
    /// <param name="userId">Out variable chứa User ID nếu thành công</param>
    /// <param name="errorResult">Out variable chứa IActionResult (401/403/400) nếu từ chối</param>
    /// <returns>True nếu hợp lệ, False nếu từ chối</returns>
    protected bool ValidateUserAndBranchToken(long targetBranchId, out long userId, out IActionResult? errorResult)
    {
        errorResult = null;
        if (!ValidateUserToken(out userId, out errorResult))
        {
            return false;
        }

        if (targetBranchId <= 0)
        {
            errorResult = BadRequest(new { 
                success = false, 
                message = "Yêu cầu không hợp lệ: Chi nhánh (BranchId) phải lớn hơn 0." 
            });
            return false;
        }

        // Block write operations if the branch is "Ngừng kinh doanh"
        string method = HttpContext.Request.Method;
        bool isWriteOperation = method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH";
        if (isWriteOperation)
        {
            var dbContext = HttpContext.RequestServices.GetService(typeof(MenuGoBE.Data.AppDbContext)) as MenuGoBE.Data.AppDbContext;
            if (dbContext != null)
            {
                var branch = dbContext.Branches.Find(targetBranchId);
                if (branch != null && branch.Status == "Ngừng kinh doanh")
                {
                    errorResult = BadRequest(new {
                        success = false,
                        message = "Truy cập bị từ chối: Chi nhánh này đã ngừng kinh doanh. Không thể thực hiện các thao tác thay đổi dữ liệu."
                    });
                    return false;
                }
            }
        }

        // 1. Vai trò Admin và Owner: Cho phép thao tác với kho dựa trên branchid truyền vào từ bên ngoài
        bool isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner") ||
                             User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                             User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");

        if (isAdminOrOwner)
        {
            return true;
        }

        // 2. Chỉ có vai trò Manager mới được phép thao tác kho (kiểm tra theo branchId trong token). Toàn bộ các role khác (Cashier, Chef, Waiter) đều bị cấm.
        bool isManager = User.IsInRole("Manager") || User.HasClaim(ClaimTypes.Role, "Manager") || User.HasClaim("role", "Manager");
        if (!isManager)
        {
            errorResult = StatusCode(403, new { 
                success = false, 
                message = "Truy cập bị từ chối: Chỉ có vai trò Admin, Owner hoặc Manager mới được phép thao tác trên chứng từ kho." 
            });
            return false;
        }

        // 3. Vai trò Manager: Bắt buộc targetBranchId phải nằm trong danh sách branchId của token
        var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId")).Select(c => c.Value);
        if (branchClaims.Contains(targetBranchId.ToString()))
        {
            return true;
        }

        errorResult = StatusCode(403, new { 
            success = false, 
            message = $"Truy cập bị từ chối: Tài khoản Quản lý của bạn không có quyền thao tác trên Chi nhánh ID: {targetBranchId}." 
        });
        return false;
    }
    #endregion
}
#endregion
