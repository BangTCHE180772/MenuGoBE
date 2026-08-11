# Tài Liệu Tổng Quan Hệ Thống Chứng Từ (Document System Overview)

Tài liệu này quy định các quy tắc chung, vòng đời trạng thái, cơ chế khóa dữ liệu, tính toán số học và cơ chế đóng băng dữ liệu (Snapshot Freeze) áp dụng nhất quán trên toàn bộ các chứng từ trong hệ thống MenuGoBE.

---

## 1. Controller Sử Dụng Dùng Chung

### `DocumentController.cs`
- **Route Base:** `/api/document`
- **Chức năng:** Cung cấp các API truy vấn đọc dữ liệu (GET) dùng chung cho tất cả loại chứng từ.

### Các Endpoints:
1. `GET /api/document?branchId={branchId}&type={type}&status={status}`
   - **Mục đích:** Lấy danh sách chứng từ dạng bảng (Grid View) theo chi nhánh.
   - **Tham số:** `branchId` (bắt buộc), `type` (tùy chọn enum DocumentType), `status` (tùy chọn enum DocumentStatus).
   - **Ràng buộc:** Token người dùng phải có quyền truy cập vào `branchId`.
2. `GET /api/document/{id}`
   - **Mục đích:** Lấy chi tiết toàn bộ chứng từ bất kỳ dưới dạng `DocumentResponseDto` (gồm Header, Details, Partner, CashFlows).

---

## 2. Vòng Đời Trạng Thái Chứng Từ (Document Status Lifecycle)

| Mã Trạng Thái | Tên Trạng Thái (Enum) | Giá Trị Số | Ý Nghĩa & Khả Năng Thao Tác |
| :---: | :--- | :---: | :--- |
| **Pending** | `DocumentStatus.Pending` | `1` | **Lưu tạm (Nháp):** Chưa ảnh hưởng tới kho hay sổ quỹ. Cho phép Sửa (PUT), Xóa mềm (DELETE). Master Data Snapshot chưa được đóng băng. |
| **Completed** | `DocumentStatus.Completed` | `2` | **Đã Chốt:** Đã ghi Sổ kho (`InventoryLedger`), Sổ quỹ (`CashFlow`), tính lại giá vốn. **Khóa tuyệt đối:** Không cho sửa, xóa hay thay đổi. |
| **InTransit** | `DocumentStatus.InTransit` | `3` | **Đang Vận Chuyển:** Dành riêng cho phiếu **Chuyển kho (`Transfer`)**. Hàng đã xuất khỏi kho gửi nhưng chưa nhập kho nhận. |
| **Cancelled** | `DocumentStatus.Cancelled` | `4` | **Đã Hủy:** Phiếu nháp đã bị xóa mềm. **Khóa tuyệt đối:** Lưu trữ vết hủy và lý do hủy (`DeleteNote`). |

---

## 3. Các Ràng Buộc Hệ Thống & Bảo Vệ Dữ Liệu (Universal Constraints)

### 3.1. Khóa Tuyệt Đối (Document Lock Guard)
- Khi chứng từ đạt trạng thái `Completed` hoặc `Cancelled`, hệ thống kích hoạt hàm `ValidateDocumentNotLocked()`.
- **Hành vi:** Ngăn chặn và quăng ngoại lệ `MenuGoException` (mã lỗi `DocumentLockedCompleted` / `DocumentLockedCancelled`) nếu có bất kỳ yêu cầu UPDATE/DELETE nào.

### 3.2. Ràng Buộc Phụ Thuộc Chứng Từ Gốc (Parent Dependency Rule)
- **Phiếu Trả Hàng NCC (`Return`):** BẮT BUỘC phải trỏ tới `ParentDocumentId` của một phiếu **Nhập kho (`Import`)** đã `Completed`.
- **Phiếu Khách Trả Hàng (`CustomerReturn`):** BẮT BUỘC phải trỏ tới `ParentDocumentId` của một phiếu **Xuất bán (`Sale`)** đã `Completed`.

### 3.3. Quy Tắc Giới Hạn Số Học & Cấu Trúc Phiếu
1. **Độ dài văn bản:**
   - `Note` (Ghi chú phiếu) <= 255 ký tự (`DocumentNoteTooLong`).
   - `DeleteNote` (Lý do hủy) <= 255 ký tự (`DocumentDeleteNoteTooLong`).
2. **Chi tiết chứng từ (DocumentDetails):**
   - Bắt buộc phải có tối thiểu 1 dòng chi tiết (`DocumentMustHaveDetail`).
   - Khống chế **Không trùng lặp `BInventoryId`** trong cùng 1 phiếu (`DocumentDuplicateDetail`).
3. **Khoảng số lượng & đơn giá:**
   - **Số lượng (Quantity):** `0.001` <= Quantity <= `10,000,000,000` (10 tỷ) (`DocumentQuantityRangeExceeded`).
   - **Đơn giá (UnitPrice):** `0` <= UnitPrice <= `10,000,000,000` VNĐ (`DocumentInvalidUnitPrice`).
   - **Tổng tiền (TotalAmount):** <= `10,000,000,000` VNĐ (10 tỷ) (`DocumentTotalAmountExceedsLimit`).

---

## 4. Công Thức Đơn Vị Quy Đổi & Số Lượng Kho Cơ Sở

- Hệ thống hỗ trợ quản lý theo Đơn vị quy đổi (`UnitConversion`).
- **Công thức tính số lượng kho cơ sở (`BaseQuantity`):**
  $$\text{BaseQuantity} = \text{Quantity} \times \text{ConversionRate}$$
  - Trong đó `ConversionRate = UnitConversion.ConversionPoint` (nếu chọn đơn vị quy đổi), ngược lại `ConversionRate = 1`.
- Client truyền `Quantity` theo đơn vị hiển thị, Hệ thống tự động quy đổi ra `BaseQuantity` trước khi ghi Sổ kho.

---

## 5. Quy Tắc Làm Tròn Tiền & Phát Sinh Sổ Quỹ (Document Rounding & CashFlow)

### 5.1. Công Thức Làm Tròn Tiền
Khi tính toán tổng tiền chứng từ (`TotalAmount`), hệ thống làm tròn theo quy tắc:
- Phần thập phân $< 0.5$: Làm tròn xuống đơn vị tiền tệ.
- Phần thập phân $\ge 0.5$: Làm tròn lên đơn vị tiền tệ.
- **Giá trị làm tròn (`RoundingValue`):** $\text{RoundingValue} = \text{RawTotal} - \text{RoundedTotal}$.

### 5.2. Thanh Toán & Công Nợ
- **Tiền phải trả (`AmountDue`):** $\text{AmountDue} = \text{RoundedTotal} - \text{AmountPaid}$.
- Nếu `AmountPaid > 0` tại thời điểm chốt phiếu: Tự động phát sinh 1 bản ghi `CashFlow` (Thu/Chi) ghi nhận số tiền thanh toán thực tế.

---

## 6. Cơ Chế Đóng Bằng Dữ Liệu Master Data (Snapshot Freezing)

Để đảm bảo tính toàn vẹn báo cáo lịch sử (ngay cả khi Tên Chi Nhánh, Tên Sản Phẩm, Tên Đơn Vị Tính hoặc Tên Nhân Viên bị thay đổi/xóa trong tương lai), khi Chốt phiếu (`Completed`) hoặc Hủy phiếu (`Cancelled`), hệ thống thực hiện **Đóng Băng Master Data**:
- **Phiếu Header:** Đóng băng `SnapshotBranchName`, `SnapshotPartnerName`, `SnapshotCreatedByName`, `SnapshotPostedByName`.
- **Phiếu Details:** Đóng băng `SnapshotProductName`, `SnapshotUnitName`, `SnapshotAvgCost`, `SnapshotConversionRate`.
