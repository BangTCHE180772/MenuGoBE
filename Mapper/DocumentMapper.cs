using System.Collections.Generic;
using System.Linq;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Mapper
{
    /// <summary>
    /// Extension class cung cấp phương thức Mapping chuyển đổi từ Document entity sang DocumentResponseDto và DocumentListResponseDto.
    /// Hỗ trợ cả hai trạng thái: COMPLETED (Snapshot) và PENDING (Current Live Data & Ref IDs).
    /// </summary>
    public static class DocumentMapper
    {
        /// <summary>
        /// Chuyển đổi từ Document Entity sang DocumentListResponseDto (Dùng cho màn hình Danh sách dạng bảng).
        /// </summary>
        public static DocumentListResponseDto ToListDto(this Document document)
        {
            if (document == null) return null!;

            var validDetails = document.DocumentDetails?.Where(dt => !dt.IsDeleted) ?? Enumerable.Empty<DocumentDetail>();

            return new DocumentListResponseDto
            {
                Id = document.Id,
                BranchId = document.BranchId,
                ToBranchId = document.ToBranchId,
                PartnerId = document.PartnerId,
                ParentDocumentId = document.ParentDocumentId,
                ParentDocumentCode = document.ParentDocument?.Code,
                PostingSequence = document.PostingSequence,
                Code = document.Code,
                Type = document.Type,
                Status = document.Status,
                TransferStatus = document.TransferStatus,
                OrderDate = document.OrderDate,
                CreatedAt = document.CreatedAt,
                ResponseDate = document.ResponseDate,

                CreatedBy = document.CreatedBy,
                SnapshotCreatedByName = document.SnapshotCreatedByName,
                CurrentCreatedByName = document.Creator?.Name,

                SnapshotBranchName = document.SnapshotBranchName,
                CurrentBranchName = document.Branch?.Name,

                SnapshotToBranchName = document.SnapshotToBranchName,
                CurrentToBranchName = document.ToBranch?.Name,

                SnapshotPartnerName = document.SnapshotPartnerName,
                CurrentPartnerName = document.Partner?.Name,

                TotalQuantity = document.Type == DocumentType.Production
                    ? validDetails.Where(dt => dt.FatherId == null).Sum(dt => dt.Quantity)
                    : validDetails.Sum(dt => dt.Quantity),
                TotalAmount = document.TotalAmount > 0
                    ? document.TotalAmount
                    : (document.Type == DocumentType.Production
                        ? validDetails.Where(dt => dt.FatherId == null).Sum(dt => dt.Quantity * (dt.UnitPrice > 0
                            ? dt.UnitPrice
                            : ((dt.SnapshotAvgCost > 0 ? dt.SnapshotAvgCost : (dt.BInventory != null ? dt.BInventory.Avg : 0m)) * (dt.ConversionRate > 0 ? dt.ConversionRate : 1m))
                        ))
                        : validDetails.Sum(dt => dt.Quantity * (dt.UnitPrice > 0
                            ? dt.UnitPrice
                            : ((dt.SnapshotAvgCost > 0 ? dt.SnapshotAvgCost : (dt.BInventory != null ? dt.BInventory.Avg : 0m)) * (dt.ConversionRate > 0 ? dt.ConversionRate : 1m))
                        ))),
                AmountPaid = document.AmountPaid,
                AmountDue = document.AmountDue,
                Note = document.Note
            };
        }

        /// <summary>
        /// Chuyển đổi từ Document Entity sang DocumentResponseDto (Dùng chung cho màn hình Xem chi tiết tất cả loại chứng từ).
        /// </summary>
        public static DocumentResponseDto ToResponseDto(this Document document)
        {
            if (document == null) return null!;

            var isPending = document.Status == DocumentStatus.Pending;

            var dto = new DocumentResponseDto
            {
                Id = document.Id,
                BranchId = document.BranchId,
                ToBranchId = document.ToBranchId,
                PartnerId = document.PartnerId,
                ParentDocumentId = document.ParentDocumentId,
                ParentDocumentCode = document.ParentDocument?.Code,
                PostingSequence = document.PostingSequence,

                Code = document.Code,
                Type = document.Type,
                Status = document.Status,
                TransferStatus = document.TransferStatus,
                OrderDate = document.OrderDate,

                SnapshotBranchName = document.SnapshotBranchName,
                SnapshotToBranchName = document.SnapshotToBranchName,
                SnapshotPartnerName = document.SnapshotPartnerName,

                CurrentPartnerName = document.Partner?.Name,
                CurrentBranchName = document.Branch?.Name,

                CreatedBy = document.CreatedBy,
                SnapshotCreatedByName = document.SnapshotCreatedByName,
                SnapshotCreatedByUsername = document.SnapshotCreatedByUsername,
                CreatedAt = document.CreatedAt,

                PostedBy = document.PostedBy,
                SnapshotPostedByName = document.SnapshotPostedByName,
                SnapshotPostedByUsername = document.SnapshotPostedByUsername,
                PostedAt = document.PostedAt,

                DeletedBy = document.DeletedBy,
                SnapshotDeletedByName = document.SnapshotDeletedByName,
                DeletedAt = document.DeletedAt,

                ReceiverAccountId = document.ReceiverAccountId,
                SnapshotReceiverName = document.SnapshotReceiverName,
                SnapshotReceiverUsername = document.SnapshotReceiverUsername,
                ResponseDate = document.ResponseDate,

                TotalAmount = document.TotalAmount,
                AmountDue = document.AmountDue,
                AmountPaid = document.AmountPaid,
                Note = document.Note,
                DeleteNote = document.DeleteNote
            };

            // Mapping Partner nếu có
            if (document.Partner != null)
            {
                dto.Partner = new DocumentPartnerResponseDto
                {
                    Id = document.Partner.Id,
                    Name = document.Partner.Name,
                    Phone = document.Partner.Phone,
                    Email = document.Partner.Email,
                    Type = document.Partner.Type,
                    Address = document.Partner.Address != null
                        ? (document.Partner.Address.NewWard != null 
                            ? $"{document.Partner.Address.NewWard.Name}, {document.Partner.Address.NewWard.NewProvince?.Name}"
                            : $"{document.Partner.Address.OldWard?.Name}, {document.Partner.Address.OldWard?.OldDistrict?.Name}, {document.Partner.Address.OldWard?.OldDistrict?.OldProvince?.Name}")
                        : null
                };
            }

            // Mapping CashFlows liên quan
            if (document.CashFlows != null && document.CashFlows.Any())
            {
                dto.CashFlows = document.CashFlows
                    .Where(cf => !cf.IsDeleted)
                    .Select(cf => new DocumentCashFlowResponseDto
                    {
                        Id = cf.Id,
                        BranchId = cf.BranchId,
                        DocumentId = cf.DocumentId,
                        PartnerId = cf.PartnerId,
                        PostingSequence = cf.PostingSequence,
                        Code = cf.Code,
                        BusinessDate = cf.BusinessDate,
                        Direction = cf.Direction,
                        Type = cf.Type,
                        PaymentMethod = cf.PaymentMethod,
                        Status = cf.Status,
                        TotalAmount = cf.TotalAmount,
                        Note = cf.Note,
                        CreatedAt = cf.CreatedAt
                    })
                    .ToList();
            }

            // Mapping InventoryLedgers liên quan
            if (document.InventoryLedgers != null && document.InventoryLedgers.Any())
            {
                dto.InventoryLedgers = document.InventoryLedgers
                    .Select(il => new DocumentInventoryLedgerResponseDto
                    {
                        Id = il.Id,
                        BInventoryId = il.BInventoryId,
                        DocumentId = il.DocumentId,
                        PostingSequence = il.PostingSequence,
                        PostedAt = il.PostedAt,
                        DocumentType = il.DocumentType,
                        PostedBy = il.PostedBy,
                        SnapshotPostedByName = il.SnapshotPostedByName,
                        SnapshotBranchName = il.SnapshotBranchName,
                        SnapshotProductName = il.SnapshotProductName,
                        SnapshotUnitName = il.SnapshotUnitName,
                        ConversionRateSnapshot = il.ConversionRateSnapshot,
                        QuantityDelta = il.QuantityDelta,
                        InventoryValueDelta = il.InventoryValueDelta,
                        RunningQuantity = il.RunningQuantity,
                        RunningInventoryValue = il.RunningInventoryValue,
                        RunningAverageCost = il.RunningAverageCost,
                        UnitCost = il.UnitCost,
                        CreatedAt = il.CreatedAt
                    })
                    .ToList();
            }

            // Mapping DocumentDetails
            if (document.DocumentDetails != null && document.DocumentDetails.Any())
            {
                dto.Details = document.DocumentDetails
                    .Where(dt => !dt.IsDeleted)
                    .Select(dt =>
                    {
                        var detailDto = new DocumentDetailResponseDto
                        {
                            Id = dt.Id,
                            DocumentId = dt.DocumentId,
                            BInventoryId = dt.BInventoryId,
                            FatherId = dt.FatherId,
                            UnitConversionId = dt.UnitConversionId,
                            ProductId = dt.BInventory != null ? (long?)dt.BInventory.ProductId : null,

                            // Snapshots
                            SnapshotProductName = dt.SnapshotProductName,
                            SnapshotProductCode = dt.SnapshotProductCode,
                            SnapshotUnitName = dt.SnapshotUnitName,
                            SnapshotBaseUnitName = dt.SnapshotBaseUnitName,
                            SnapshotAvgCost = dt.SnapshotAvgCost,
                            BatchCodeSnapshot = dt.BatchCodeSnapshot,
                            ManufactureDateSnapshot = dt.ManufactureDateSnapshot,
                            ExpiryDateSnapshot = dt.ExpiryDateSnapshot,

                            // Fallback Display Names (Đảm bảo FE luôn nhận được tên/mã/đơn vị tính dù là phiếu đã chốt hay phiếu nháp)
                            ProductName = !string.IsNullOrWhiteSpace(dt.SnapshotProductName) 
                                ? dt.SnapshotProductName 
                                : (dt.BInventory?.Product?.Name ?? string.Empty),
                            ProductCode = !string.IsNullOrWhiteSpace(dt.SnapshotProductCode) 
                                ? dt.SnapshotProductCode 
                                : (dt.BInventory?.Product?.SKUCode ?? string.Empty),
                            UnitName = !string.IsNullOrWhiteSpace(dt.SnapshotUnitName) 
                                ? dt.SnapshotUnitName 
                                : (dt.UnitConversion?.Unit?.Name ?? dt.BInventory?.Product?.UnitConversions?.FirstOrDefault(uc => uc.BaseId == null)?.Unit?.Name ?? string.Empty),
                            BaseUnitName = !string.IsNullOrWhiteSpace(dt.SnapshotBaseUnitName) 
                                ? dt.SnapshotBaseUnitName 
                                : (dt.BInventory?.Product?.UnitConversions?.FirstOrDefault(uc => uc.BaseId == null)?.Unit?.Name ?? string.Empty),

                            // Quantities & Prices
                            ConversionRate = dt.ConversionRate,
                            Quantity = dt.Quantity,
                            BaseQuantity = dt.BaseQuantity,
                            UnitPrice = dt.UnitPrice,
                            TotalPrice = dt.Quantity * dt.UnitPrice,

                            // Special quantities
                            SystemQuantity = dt.SystemQuantity,
                            ActualQuantity = dt.ActualQuantity,
                            AdjustedCostDelta = dt.AdjustedCostDelta,
                            NewAvgCost = dt.NewAvgCost,
                            ReceivedQuantity = dt.ReceivedQuantity,

                            IsDeleted = dt.IsDeleted,
                            Note = dt.Note,
                            CreatedAt = dt.CreatedAt
                        };

                        // Bổ sung dữ liệu Current Live Data từ BInventory & Product & UnitConversion nếu có
                        if (dt.BInventory != null)
                        {
                            var baseUnitConv = dt.BInventory.Product?.UnitConversions?.FirstOrDefault(uc => uc.BaseId == null);

                            detailDto.CurrentProductName = dt.BInventory.Product?.Name;
                            detailDto.CurrentProductCode = dt.BInventory.Product?.SKUCode;
                            detailDto.CurrentUnitName = dt.UnitConversion != null 
                                ? dt.UnitConversion.Unit?.Name 
                                : baseUnitConv?.Unit?.Name;
                            detailDto.CurrentBaseUnitName = baseUnitConv?.Unit?.Name;
                            detailDto.CurrentConversionRate = dt.UnitConversion != null ? dt.UnitConversion.ConversionPoint : 1;
                            detailDto.CurrentStockQuantity = dt.BInventory.Quantity;
                        }

                        return detailDto;
                    })
                    .ToList();
            }

            return dto;
        }
    }
}
