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

    public static readonly ErrorResult UnitConversionInUseCannotBeDeleted = new(
        1042,
        "Đơn vị quy đổi đã phát sinh chứng từ kho, không thể xóa.",
        400
    );

    public static readonly ErrorResult InvalidConversionPoint = new(
        1016,
        "Giá trị quy đổi của đơn vị phụ phải là số nguyên từ 2 đến 1.000.000 (1 triệu).",
        400
    );

    public static readonly ErrorResult SellPriceRequired = new(
        1017,
        "Giá bán phải lớn hơn 0 đối với sản phẩm loại Processed, Manufactured và Regular.",
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
        "Sản phẩm loại Processed không được phép cấu hình đơn vị quy đổi.",
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

    public static readonly ErrorResult SellPriceNotAllowedForIngredientOrTool = new(
        1036,
        "Sản phẩm loại Nguyên liệu và Dụng cụ không được phép có giá bán.",
        400
    );

    public static readonly ErrorResult IngredientOrToolIsSellableNotAllowed = new(
        1037,
        "Sản phẩm loại Nguyên liệu và Dụng cụ không được phép đặt thuộc tính có thể bán.",
        400
    );

    public static readonly ErrorResult IngredientOrToolMenuNotAllowed = new(
        1038,
        "Sản phẩm loại Nguyên liệu và Dụng cụ không được phép thuộc về bất kỳ thực đơn nào.",
        400
    );

    public static readonly ErrorResult SellPriceExceedsLimit = new(
        1039,
        "Giá bán không được vượt quá 10 tỷ đồng.",
        400
    );

    public static readonly ErrorResult CategoryRequired = new(
        1040,
        "Sản phẩm bắt buộc phải thuộc về một nhóm sản phẩm (GroupId).",
        400
    );

    public static readonly ErrorResult DuplicateRecipeIngredient = new(
        1041,
        "Thành phần trong công thức không được trùng lặp.",
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

    public static readonly ErrorResult BranchAlreadyDeleted = new(
        2020,
        "Chi nhánh đã ngừng kinh doanh (đã xóa), không thể thay đổi hoặc cập nhật.",
        400
    );
    
    public static readonly ErrorResult DocumentBranchNotFound = new(
        2101,
        "Chi nhánh không tồn tại.",
        404
    );

    public static readonly ErrorResult DocumentBranchInactive = new(
        2170,
        "Chi nhánh đã ngừng kinh doanh, không thể thực hiện giao dịch hoặc chuyển hàng.",
        400
    );

    public static readonly ErrorResult DocumentNotFound = new(
        2108, 
        "Không tìm thấy chứng từ.", 
        404
    );

    public static readonly ErrorResult DocumentAlreadyDeleted = new(
        2109, 
        "Chứng từ đã bị xóa trước đó.", 
        400
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

    public static readonly ErrorResult DocumentCannotDeleteHasReturn = new(
        2107, 
        "Không thể xóa phiếu nhập vì đã có phiếu trả hàng tham chiếu đến nó.", 
        400
    );

    public static readonly ErrorResult DocumentInvalidReturnBusinessDate = new(
        2110, 
        "Ngày trả hàng không được nhỏ hơn ngày của phiếu nhập gốc.", 
        400
    );

    public static readonly ErrorResult DocumentReturnQuantityExceeded = new(
        2111, 
        "Số lượng trả hàng không được vượt quá số lượng đã nhập.", 
        400
    );

    public static readonly ErrorResult DocumentPartnerNotFound = new(
        2112,
        "Không tìm thấy đối tác trong hệ thống.",
        404
    );

    public static readonly ErrorResult DocumentAmountDueRequiredForNonSupplier = new(
        2113,
        "Đối tác loại Vận chuyển / Khác phải khai báo số tiền cần trả (AmountDue).",
        400
    );

    public static readonly ErrorResult DocumentTotalAmountExceedsLimit = new(
        2114,
        "Tổng giá trị chứng từ / chuyến chuyển kho không được vượt quá 10 tỷ đồng.",
        400
    );

    public static readonly ErrorResult DocumentUnitConversionNotFound = new(
        2115,
        "Không tìm thấy quy đổi đơn vị tương ứng.",
        404
    );

    public static readonly ErrorResult DocumentInvalidQuantity = new(
        2116,
        "Số lượng không hợp lệ: tất cả các dòng chi tiết đều bằng 0.",
        400
    );

    public static readonly ErrorResult DocumentReturnInsufficientInventory = new(
        2117,
        "Tồn kho không đủ để trả hàng tại thời điểm được chọn.",
        400
    );

    public static readonly ErrorResult DocumentTransferReceiveDateMustBeAfterSendDate = new(
        2118,
        "Ngày nhận hàng phải lớn hơn ngày xuất chuyển từ chi nhánh gửi.",
        400
    );

    public static readonly ErrorResult DocumentTransferFromBranchPostingLockError = new(
        2119,
        "Không thể thực hiện do chi nhánh gửi đã khóa sổ.",
        400
    );

    public static readonly ErrorResult DocumentProductionInsufficientRawMaterial = new(
        2120,
        "Tồn kho nguyên vật liệu không đủ tại thời điểm sản xuất.",
        400
    );

    public static readonly ErrorResult DocumentInvalidUnitPrice = new(
        2121,
        "Đơn giá/số tiền phải là số nguyên lớn hơn hoặc bằng 1.",
        400
    );

    public static readonly ErrorResult DocumentQuantityRangeExceeded = new(
        2122,
        "Số lượng hàng hóa phải từ 0,001 đến 1.000.000 đơn vị.",
        400
    );

    public static readonly ErrorResult DocumentAmountPaidExceedsAmountDue = new(
        2123,
        "Số tiền đã trả không được vượt quá số tiền phải trả.",
        400
    );

    /// <summary>
    /// ERR_STOCK_AUDIT_LOCKED – BusinessDate của chứng từ phải lớn hơn (>)
    /// BusinessDate của phiếu kiểm kho (Chốt sổ) mới nhất cho từng sản phẩm liên quan.
    /// </summary>
    public static readonly ErrorResult DocumentStockAuditLocked = new(
        2124,
        "Ngày chứng từ phải lớn hơn ngày phiếu kiểm kho chốt sổ gần nhất của tất cả sản phẩm liên quan.",
        400
    );

    public static readonly ErrorResult DocumentDuplicatePartner = new(
        2125,
        "Mỗi đối tác chỉ được xuất hiện một lần trong chứng từ.",
        400
    );

    public static readonly ErrorResult DocumentBusinessDateInFutureError = new(
        2126,
        "Ngày chứng từ không được lớn hơn thời gian hiện tại.",
        400
    );

    public static readonly ErrorResult DocumentCannotUpdateAlreadyCompleted = new(
        2127,
        "Chỉ có thể cập nhật phiếu ở trạng thái Chờ (Pending).",
        400
    );

    public static readonly ErrorResult DocumentOtherCostMustBeInteger = new(
        2128,
        "Chi phí khác phải là số nguyên lớn hơn hoặc bằng 0.",
        400
    );

    public static readonly ErrorResult DocumentOtherCostNoteRequired = new(
        2129,
        "Khi có chi phí khác, ghi chú không được để trống.",
        400
    );

    public static readonly ErrorResult DocumentOtherCostExceedsLimit = new(
        2130,
        "Chi phí khác không được vượt quá 10 tỷ đồng.",
        400
    );

    // ──────────────────────────────────────────────────────────────────────
    // Document Service Exceptions (2131–2162)
    // ──────────────────────────────────────────────────────────────────────

    public static readonly ErrorResult DocumentNullInfo = new(
        2131,
        "Thông tin chứng từ không được để trống.",
        400
    );

    public static readonly ErrorResult DocumentNoteTooLong = new(
        2132,
        "Ghi chú chứng từ (Note) không được vượt quá 255 ký tự.",
        400
    );

    public static readonly ErrorResult DocumentDeleteNoteTooLong = new(
        2133,
        "Lý do xóa chứng từ (DeleteNote) không được vượt quá 255 ký tự.",
        400
    );

    public static readonly ErrorResult DocumentMustHaveDetail = new(
        2134,
        "Chứng từ phải có tối thiểu 1 mặt hàng chi tiết.",
        400
    );

    public static readonly ErrorResult DocumentDuplicateDetail = new(
        2135,
        "Mặt hàng bị trùng lặp trong chứng từ. Mỗi mặt hàng chỉ được xuất hiện 1 lần.",
        400
    );

    public static readonly ErrorResult DocumentParentRequired = new(
        2136,
        "Chứng từ này bắt buộc phải liên kết với chứng từ gốc.",
        400
    );

    public static readonly ErrorResult DocumentParentNotCompleted = new(
        2137,
        "Chứng từ gốc chưa ở trạng thái chốt kho (Completed).",
        400
    );

    public static readonly ErrorResult DocumentInsufficientInventory = new(
        2138,
        "Tồn kho không đủ để thực hiện thao tác này.",
        400
    );

    public static readonly ErrorResult DocumentLockedCompleted = new(
        2139,
        "Chứng từ đã CHỐT (Completed). Không được phép chỉnh sửa hoặc xóa.",
        400
    );

    public static readonly ErrorResult DocumentLockedCancelled = new(
        2140,
        "Chứng từ đã bị HỦY (Canceled). Không được phép thao tác.",
        400
    );

    public static readonly ErrorResult DocumentNotPending = new(
        2141,
        "Chỉ được phép thao tác khi chứng từ ở trạng thái Pending.",
        400
    );

    public static readonly ErrorResult DocumentCodeDuplicate = new(
        2142,
        "Mã chứng từ đã tồn tại trong hệ thống. Vui lòng nhập mã khác.",
        409
    );

    public static readonly ErrorResult DocumentInventoryNotLinked = new(
        2143,
        "BInventory không liên kết với sản phẩm hợp lệ.",
        400
    );

    public static readonly ErrorResult DocumentProductTypeNotAllowed = new(
        2144,
        "Loại sản phẩm này không được phép trong loại chứng từ này.",
        400
    );

    public static readonly ErrorResult DocumentTransferBranchRequired = new(
        2145,
        "Phiếu chuyển kho phải chỉ định chi nhánh nhận (ToBranchId).",
        400
    );

    public static readonly ErrorResult DocumentTransferBranchSame = new(
        2146,
        "Chi nhánh nhận không được trùng với chi nhánh gửi.",
        400
    );

    public static readonly ErrorResult DocumentParentDetailNotFound = new(
        2147,
        "Không tìm thấy dòng chi tiết của chứng từ gốc.",
        404
    );

    public static readonly ErrorResult DocumentReturnAllAlreadyReturned = new(
        2148,
        "Mặt hàng trong chứng từ gốc đã được trả hết toàn bộ số lượng.",
        400
    );

    public static readonly ErrorResult DocumentFatherIdRequired = new(
        2149,
        "Mỗi dòng chi tiết phải chỉ định FatherId trỏ đến chứng từ gốc.",
        400
    );

    public static readonly ErrorResult DocumentRecipeNotFound = new(
        2150,
        "Sản phẩm chế biến (Processed) hoặc sản xuất (Manufactured) không có công thức chi tiết (Recipe).",
        400
    );

    public static readonly ErrorResult DocumentIngredientNotFound = new(
        2151,
        "Không tìm thấy sản phẩm nguyên liệu trong công thức.",
        404
    );

    public static readonly ErrorResult DocumentCircularDependency = new(
        2152,
        "Phát hiện vòng lặp công thức (Circular Dependency) trong sản phẩm.",
        400
    );

    public static readonly ErrorResult DocumentZeroInventoryForAdjustment = new(
        2153,
        "Không thể điều chỉnh giá vốn cho sản phẩm có tồn kho bằng 0 hoặc âm.",
        400
    );

    public static readonly ErrorResult DocumentNewAvgCostOutOfRange = new(
        2154,
        "Giá vốn mới không hợp lệ. Phải nằm trong khoảng từ 0 đến 10 tỷ VNĐ.",
        400
    );

    public static readonly ErrorResult DocumentAdjustmentValueOutOfRange = new(
        2155,
        "Giá trị điều chỉnh giá vốn vượt ngoài hạn mức (±10 tỷ VNĐ).",
        400
    );

    public static readonly ErrorResult DocumentCostAdjustmentNegative = new(
        2156,
        "Giá vốn mới không được nhỏ hơn 0.",
        400
    );

    public static readonly ErrorResult DocumentAmountPaidInvalid = new(
        2157,
        "Số tiền đã trả không hợp lệ.",
        400
    );

    public static readonly ErrorResult DocumentSupplierRequiredForPayment = new(
        2158,
        "Khi phát sinh tiền đã trả (AmountPaid > 0), bắt buộc phải chọn Nhà cung cấp.",
        400
    );

    public static readonly ErrorResult DocumentSupplierRequiredForComplete = new(
        2159,
        "Khi chốt phiếu nhập kho, bắt buộc phải chọn Nhà cung cấp (Supplier).",
        400
    );

    public static readonly ErrorResult DocumentProductionInfoRequired = new(
        2160,
        "Phiếu sản xuất phải có thông tin sản phẩm sản xuất (Manufactured).",
        400
    );

    public static readonly ErrorResult DocumentTransferReceiptStatusInvalid = new(
        2161,
        "Trạng thái nhận hàng không hợp lệ. Chỉ chấp nhận Received, PartialReceived hoặc Rejected.",
        400
    );

    public static readonly ErrorResult DocumentTransferReceiptNoteRequired = new(
        2162,
        "Bắt buộc phải nhập Lý do / Ghi chú (Note) khi xác nhận nhận hàng hoặc từ chối nhận hàng.",
        400
    );

    public static readonly ErrorResult DocumentTransferPartialNotAllowed = new(
        2163,
        "Không được phép nhận một phần. Vui lòng chọn Nhận đủ hoặc Từ chối nhận.",
        400
    );

    public static readonly ErrorResult PayrollSuggestionNotFound = new(
        2201,
        "Không tìm thấy kiến nghị lương.",
        404
    );

    public static readonly ErrorResult PayrollSuggestionInvalidData = new(
        2202,
        "Dữ liệu kiến nghị lương không hợp lệ.",
        400
    );
}



