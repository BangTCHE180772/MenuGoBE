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
        "ConversionPoint của đơn vị phụ phải là số nguyên lớn hơn 1 và nhỏ hơn hoặc bằng 1 tỷ.", 
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
        "Số lượng nguyên liệu phải lớn hơn 0, nhỏ hơn hoặc bằng 1 tỷ, và tối đa 3 chữ số thập phân.", 
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
        "Không thể cập nhật hoặc xóa công thức đã ghi nhận trong đơn hàng (OrderDetailed).", 
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
        "Sản phẩm loại processed chỉ được cấu hình đơn vị cơ sở (base unit), không được thêm các đơn vị chuyển đổi khác.", 
        400
    );
}
