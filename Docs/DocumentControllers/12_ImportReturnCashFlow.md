# Tài Liệu Dòng Tiền Gắn Với Chứng Từ Nhập Kho & Trả Hàng NCC (Import & Return CashFlow Integration)

**Module:** Document Management & CashFlow Integration  
**Controller Liên Quan:** `ImportDocumentController.cs`, `ReturnDocumentController.cs`, `CashFlowController.cs`  
**Đối tượng tích hợp:** `Document` (Import/Return), `CashFlow`, `Partner` (Supplier)

---

## 1. Mục Đích Nghiệp Vụ

Tài liệu này mô tả cơ chế liên kết tự động và các quy tắc quản lý dòng tiền (**CashFlow - Sổ Quỹ**) phát sinh trực tiếp từ các giao dịch kho với Nhà cung cấp (Supplier), bao gồm:
1. **Giao dịch Nhập kho (`Import`):** Ghi nhận khoản chi trả tiền mua hàng cho Nhà cung cấp.
2. **Giao dịch Trả hàng Nhà cung cấp (`Return`):** Ghi nhận khoản thu hoàn tiền từ Nhà cung cấp khi trả lại hàng hóa/nguyên vật liệu.
3. **Quy tắc kiểm soát hạn mức dòng tiền:** Đảm bảo tổng số tiền chi/thu lũy kế qua các phiếu CashFlow không được vượt quá tổng giá trị chứng từ phải trả hoặc phải nhận.

---

## 2. Chi Tiết Dòng Tiền Theo Loại Chứng Từ

### 2.1. Phiếu Nhập Kho (`ImportDocument`) & Dòng Tiền
- **Chiều dòng tiền (Direction):** Khi người dùng nhập hàng và thực hiện thanh toán cho Nhà cung cấp (`AmountPaid > 0`), hệ thống tự động sinh phiếu chi thuộc Sổ quỹ với chiều **Chi tiền (Outflow)**. *(Lưu ý: Trên giao diện hoặc góc độ ghi nhận sổ phiếu, thông tin tiền nhập kho được liên kết trực tiếp với chi phí đầu vào của chi nhánh).*
- **Mã phiếu CashFlow tự động:** Sinh theo cấu trúc `CF_{DocumentCode}` (Ví dụ: `CF_NH000123`).
- **Quy tắc Partner bắt buộc:** 
  - Ngay cả ở trạng thái nháp (`Pending`), nếu có phát sinh `AmountPaid > 0` thì **BẮT BUỘC** phải chọn Nhà cung cấp (`PartnerId`).
  - Khi chốt phiếu Nhập kho (`Completed`), **BẮT BUỘC** phải chọn Nhà cung cấp (`PartnerId`).

### 2.2. Phiếu Trả Hàng Nhà Cung Cấp (`ReturnDocument`) & Dòng Tiền
- **Chiều dòng tiền (Direction):** Khi người dùng xuất trả hàng lại cho Nhà cung cấp và nhận lại tiền mặt/chuyển khoản (`AmountPaid > 0`), hệ thống tự động sinh phiếu thu thuộc Sổ quỹ với chiều **Thu tiền (Inflow)**.
- **Mã phiếu CashFlow tự động:** Sinh theo cấu trúc `CF_{DocumentCode}` (Ví dụ: `CF_TH000045`).
- **Ràng buộc phiếu gốc:** Bắt buộc gắn với `ParentDocumentId` là phiếu Nhập kho (`Import`) đã ở trạng thái `Completed`.

---

## 3. Ràng Buộc Kiểm Soát Hạn Mức Thanh Toán Lũy Kế (Cumulative Limit Constraint)

Để tránh thất thoát dòng tiền và sai lệch công nợ, hệ thống áp dụng cơ chế kiểm soát số tiền thanh toán nghiêm ngặt:

### 3.1. Quy Tắc Giới Hạn Số Tiền
- **Đối với Phiếu Nhập kho (`Import`):**
  $$\sum \text{CashFlow}_{\text{Outflow}} \le \text{Document.RoundedTotalAmount}$$
  - Tổng số tiền đã trả cho NCC qua tất cả các đợt thanh toán (phiếu CashFlow) không được vượt quá tổng tiền phiếu nhập sau làm tròn (`RoundedTotalAmount`).

- **Đối với Phiếu Trả hàng NCC (`Return`):**
  $$\sum \text{CashFlow}_{\text{Inflow}} \le \text{Document.RoundedTotalAmount}$$
  - Tổng số tiền nhận lại từ NCC không được vượt quá tổng giá trị hàng hóa trả lại sau làm tròn.

### 3.2. Công Thức Tính Công Nợ Tức Thời (AmountDue)
$$\text{AmountDue} = \max\left(0, \text{RoundedTotalAmount} - \sum \text{TotalCashFlowPaid}\right)$$

- Nếu người dùng nhập `AmountPaid < 0` hoặc `AmountPaid > RoundedTotalAmount`, hệ thống chặn lại và quăng ngoại lệ `DocumentAmountPaidInvalid`.
- Khi $\text{AmountDue} = 0$, chứng từ chuyển sang trạng thái **Đã thanh toán đủ (Fully Paid)**.

---

## 4. Xử Lý Điều Chỉnh Làm Tròn (CashFlow Rounding)

Khi giá trị thực tế của chứng từ phát sinh phần lẻ thập phân, hệ thống thực hiện quy tắc làm tròn tiêu chuẩn:
- Phần dư thập phân $< 0.5$: Làm tròn xuống hàng đơn vị.
- Phần dư thập phân $\ge 0.5$: Làm tròn lên hàng đơn vị.

$$\text{RoundingValue} = \text{RawTotalAmount} - \text{RoundedTotalAmount}$$

Nếu $\text{RoundingValue} \neq 0$, hệ thống tự động sinh thêm 1 bản ghi CashFlow phụ với `Type = CashFlowDetailType.Rounding` (`CF_RND_{DocumentCode}`) để cân bằng sổ sách kế toán.

---

## 5. Trạng Thái Thanh Toán Của Chứng Từ

| Trạng Thái Thanh Toán | Điều Kiện Dòng Tiền | Mô Tả Nghiệp Vụ |
| :--- | :--- | :--- |
| **Unpaid (Chưa thanh toán)** | $\sum \text{CashFlow} = 0$ | Nhập/Trả hàng ghi nợ 100%, chưa chi/thu tiền mặt. |
| **Partially Paid (Thanh toán 1 phần)** | $0 < \sum \text{CashFlow} < \text{RoundedTotal}` | Đã thanh toán/nhận hoàn 1 phần, còn nợ lại `AmountDue > 0`. |
| **Fully Paid (Thanh toán đủ)** | $\sum \text{CashFlow} = \text{RoundedTotal}` | Đã thanh toán/nhận đủ 100% giá trị chứng từ, `AmountDue = 0`. |
