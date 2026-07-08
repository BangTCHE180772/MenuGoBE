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
}
