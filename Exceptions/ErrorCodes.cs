namespace MenuGoBE.Exceptions;

public record ErrorResult(int Code, string Message, int HttpStatusCode);

public static class ErrorCodes
{
    public static readonly ErrorResult LoginFailed = new(
        1001,
        "Login failed...",
        400
    );

    public static readonly ErrorResult DuplicateMenuCategory = new(
        1002,
        "Oh god, duplicate id... in menu category",
        409
    );

    public static readonly ErrorResult DuplicateGroupName = new(
        1003,
        "Tên Group đã tồn tại.",
        409
    );

    public static readonly ErrorResult DuplicateMenuName = new(
        1004,
        "Tên Menu đã tồn tại.",
        409
    );

    public static readonly ErrorResult DuplicateUnitName = new(
        1005,
        "Tên Đơn vị đã tồn tại.",
        409
    );

    public static readonly ErrorResult UnitInUse = new(
        1006,
        "Không thể xóa Đơn vị vì đang được sử dụng trong Chuyển đổi đơn vị.",
        400
    );

    public static readonly ErrorResult DuplicateProductName = new(
        1007,
        "Tên Sản phẩm đã tồn tại.",
        409
    );

    public static readonly ErrorResult DuplicateProductSKU = new(
        1008,
        "Mã SKU của sản phẩm đã tồn tại.",
        409
    );

    public static readonly ErrorResult BaseUnitRequired = new(
        1009,
        "Cần cấu hình tối thiểu một đơn vị là base khi cung cấp danh sách chuyển đổi đơn vị.",
        400
    );

    public static readonly ErrorResult UnitNotFound = new(
        1010,
        "Không tìm thấy Đơn vị.",
        404
    );

    public static readonly ErrorResult DuplicateUnitConversion = new(
        1015,
        "Đơn vị chuyển đổi cho sản phẩm này đã tồn tại.",
        409
    );

    public static readonly ErrorResult InvalidConversionPoint = new(
        1016,
        "Giá trị quy đổi của đơn vị phụ phải là số nguyên từ 2 đến 1.000.000 (1 triệu).",
        400
    );

    public static readonly ErrorResult SellPriceRequired = new(
        1017,
        "Giá bán phải lớn hơn 0 khi sản phẩm có thể bán (IsSellable là true).",
        400
    );

    public static readonly ErrorResult RecipeNotAllowed = new(
        1018,
        "Chỉ sản phẩm loại processed và manufactured mới được phép cấu hình công thức.",
        400
    );

    public static readonly ErrorResult InvalidRecipeIngredient = new(
        1019,
        "Loại nguyên liệu không hợp lệ cho công thức sản phẩm này.",
        400
    );

    public static readonly ErrorResult InvalidRecipeQuantity = new(
        1021,
        "Số lượng nguyên liệu trong công thức phải từ 0,001 đến 1.000.000 (1 triệu), và tối đa 3 chữ số thập phân.",
        400
    );

    public static readonly ErrorResult ProductNotFound = new(
        1011,
        "Không tìm thấy Sản phẩm.",
        404
    );

    public static readonly ErrorResult RecipeRequired = new(
        1022,
        "Sản phẩm loại manufactured và processed bắt buộc phải có công thức.",
        400
    );

    public static readonly ErrorResult RecipeCycleDetected = new(
        1023,
        "Phát hiện vòng lặp phụ thuộc trong công thức sản phẩm.",
        400
    );

    public static readonly ErrorResult OnlyOneBaseUnitAllowed = new(
        1024,
        "Chỉ cho phép duy nhất một đơn vị là base trong danh sách chuyển đổi đơn vị.",
        400
    );

    public static readonly ErrorResult RecipeHasDetails = new(
        1025,
        "Không thể cập nhật hoặc xóa công thức đã ghi nhận trong đơn hàng.",
        400
    );

    public static readonly ErrorResult CannotDeleteOnlyRecipe = new(
        1026,
        "Không thể xóa công thức duy nhất của sản phẩm manufactured hoặc processed.",
        400
    );

    public static readonly ErrorResult ActiveRecipeRequired = new(
        1027,
        "Sản phẩm phải có ít nhất một công thức kích hoạt.",
        400
    );

    public static readonly ErrorResult ProcessedUnitConversionsNotAllowed = new(
        1028,
        "Sản phẩm loại processed và manufactured chỉ được cấu hình duy nhất 1 đơn vị cơ sở (base unit), không được thêm các đơn vị chuyển đổi khác.",
        400
    );

    public static readonly ErrorResult ProductRecipeLockedDueToTransactions = new(
        1029,
        "Sản phẩm đã có phát sinh giao dịch (đơn hàng/chứng từ/kho), không thể chỉnh sửa hoặc xóa công thức món.",
        400
    );

    public static readonly ErrorResult ProductCannotBeDeletedUsedInRecipe = new(
        1030,
        "Không thể xóa sản phẩm vì đang được sử dụng làm nguyên liệu trong công thức món khác hoặc có thành phần công thức.",
        400
    );

    public static readonly ErrorResult ProductCannotBeDeletedUsedInTransactions = new(
        1031,
        "Không thể xóa sản phẩm vì đã có phát sinh giao dịch trong đơn hàng, chứng từ hoặc kho.",
        400
    );

    public static readonly ErrorResult ProductCannotBeDeletedUsedInMenu = new(
        1032,
        "Không thể xóa sản phẩm vì đang được gán vào thực đơn (Menu).",
        400
    );

    public static readonly ErrorResult ProductCannotBeDeactivatedUsedInRecipe = new(
        1033,
        "Sản phẩm này đang được sử dụng làm nguyên liệu trong công thức món khác, không thể chuyển trạng thái ngưng hoạt động.",
        400
    );

    public static readonly ErrorResult ProductCannotBeDeactivatedUsedInOrders = new(
        1034,
        "Sản phẩm này đã phát sinh trong đơn hàng/hóa đơn, không thể chuyển trạng thái ngưng hoạt động.",
        400
    );

    public static readonly ErrorResult IngredientOrRegularRequired = new(
        1035,
        "Cần có ít nhất một sản phẩm loại Hàng thường hoặc Nguyên liệu để tạo công thức món.",
        400
    );

    public static readonly ErrorResult ChainNameRequired = new(
        2001,
        "Tên chuỗi không được để trống.",
        400
    );

    public static readonly ErrorResult ChainNameLengthExceeded = new(
        2002,
        "Tên chuỗi không được quá 100 ký tự.",
        400
    );

    public static readonly ErrorResult ChainOpenCloseTimeInvalid = new(
        2003,
        "Giờ mở cửa phải nhỏ hơn giờ đóng cửa.",
        400
    );

    public static readonly ErrorResult ChainImageLengthExceeded = new(
        2004,
        "Đường dẫn Logo hoặc Background không được quá 255 ký tự.",
        400
    );

    public static readonly ErrorResult BranchNameRequired = new(
        2011,
        "Tên chi nhánh không được để trống.",
        400
    );

    public static readonly ErrorResult BranchNameLengthExceeded = new(
        2012,
        "Tên chi nhánh không được vượt quá 100 ký tự.",
        400
    );

    public static readonly ErrorResult BranchOpenCloseTimeInvalid = new(
        2013,
        "Giờ mở cửa phải nhỏ hơn giờ đóng cửa.",
        400
    );

    public static readonly ErrorResult BranchQueryPageInvalid = new(
        2014,
        "Trang phải lớn hơn 0.",
        400
    );

    public static readonly ErrorResult BranchQueryPageSizeInvalid = new(
        2015,
        "Số bản ghi trên trang phải từ 1 đến 100.",
        400
    );

    public static readonly ErrorResult BranchQuerySortByInvalid = new(
        2016,
        "Cột sắp xếp không hợp lệ (Name, CreatedAt, Id hoặc ManagerName).",
        400
    );

    public static readonly ErrorResult BranchQueryKeywordLengthExceeded = new(
        2017,
        "Từ khóa tìm kiếm không được quá 100 ký tự.",
        400
    );

    public static readonly ErrorResult BranchQueryTypeLengthExceeded = new(
        2018,
        "Loại chi nhánh tìm kiếm không được quá 50 ký tự.",
        400
    );

    public static readonly ErrorResult UserBranchIdClaimMissing = new(
        2019,
        "Tài khoản quản lý không có thông tin chi nhánh liên kết hợp lệ.",
        404
    );
    
    public static readonly ErrorResult DocumentBranchNotFound = new(
        2101,
        "Chi nhánh không tồn tại.",
        404
    );

    public static readonly ErrorResult DocumentPostingLockDateError = new(
        2102,
        "Ngày chứng từ phải sau ngày khóa sổ (PostingLockDate).",
        400
    );

    public static readonly ErrorResult DocumentProductNotInInventory = new(
        2103,
        "Một số sản phẩm không tồn tại trong kho chi nhánh này.",
        400
    );

    public static readonly ErrorResult DocumentInvalidProductTypeForImport = new(
        2104,
        "Không thể nhập kho sản phẩm do thuộc loại Manufactured hoặc Processed.",
        400
    );

    public static readonly ErrorResult DocumentExactlyOneSupplierRequired = new(
        2105,
        "Phiếu nhập kho phải có đúng 1 Nhà cung cấp (Supplier).",
        400
    );

    public static readonly ErrorResult DocumentGuestNotAllowedInImport = new(
        2106,
        "Phiếu nhập kho không được có Khách lẻ (Customer).",
        400
    );
}
