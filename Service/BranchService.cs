using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Exceptions;
using MenuGoBE.Helpers;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _repo;
        private readonly IMapper _mapper;
        private readonly AppDbContext? _context;

        public BranchService(IBranchRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public BranchService(IBranchRepository repo, IMapper mapper, AppDbContext context)
        {
            _repo = repo;
            _mapper = mapper;
            _context = context;
        }

        public async Task<List<BranchViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();

            return _mapper.Map<List<BranchViewDto>>(data);
        }

        public async Task<BranchViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);

            return _mapper.Map<BranchViewDto>(data);
        }

        public async Task<List<BranchViewDto>> SearchFilteredBranchesAsync(BranchQueryDto dto)
        {
            if (dto.Page < 1)
            {
                throw new MenuGoException(ErrorCodes.BranchQueryPageInvalid);
            }

            if (dto.PageSize < 1 || dto.PageSize > 100)
            {
                throw new MenuGoException(ErrorCodes.BranchQueryPageSizeInvalid);
            }

            if (!string.IsNullOrEmpty(dto.SortBy))
            {
                var lowerSortBy = dto.SortBy.ToLower();
                if (lowerSortBy != "name" && lowerSortBy != "createdat" && lowerSortBy != "id" && lowerSortBy != "managername")
                {
                    throw new MenuGoException(ErrorCodes.BranchQuerySortByInvalid);
                }
            }

            if (!string.IsNullOrEmpty(dto.Keyword) && dto.Keyword.Length > 100)
            {
                throw new MenuGoException(ErrorCodes.BranchQueryKeywordLengthExceeded);
            }

            if (!string.IsNullOrEmpty(dto.Type) && dto.Type.Length > 50)
            {
                throw new MenuGoException(ErrorCodes.BranchQueryTypeLengthExceeded);
            }

            var branches = await _repo.SearchFilteredBranchesAsync(dto);

            return _mapper.Map<List<BranchViewDto>>(branches);
        }

        public async Task<BranchViewDto> CreateAsync(BranchCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.BranchNameRequired);
            }

            if (dto.Name.Length > 100)
            {
                throw new MenuGoException(ErrorCodes.BranchNameLengthExceeded);
            }

            if (dto.OpenTime >= dto.CloseTime)
            {
                throw new MenuGoException(ErrorCodes.BranchOpenCloseTimeInvalid);
            }

            var entity = _mapper.Map<Branch>(dto);

            entity.Address.Type = "Chi Nhánh";
            entity.Address.OldWardId = null;
            entity.Type = "Main";
            entity.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Hoạt động" : dto.Status;
            entity.PayrollPayday = dto.PayrollPayday > 0 && dto.PayrollPayday <= 31 ? dto.PayrollPayday : 15;
            entity.IsDeleted = false;
            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            if (_context != null)
            {
                var existingProducts = await _context.Products
                    .Where(p => p.ChainId == entity.ChainId)
                    .ToListAsync();

                if (existingProducts.Count > 0)
                {
                    var existingProductIds = await _context.BInventories
                        .Where(bi => bi.BranchId == entity.Id)
                        .Select(bi => bi.ProductId)
                        .ToHashSetAsync();

                    var bInventories = existingProducts
                        .Where(p => !existingProductIds.Contains(p.Id))
                        .Select(p => new BInventory
                        {
                            BranchId = entity.Id,
                            ProductId = p.Id,
                            Type = BInventoryHelper.MapProductTypeToBInventoryType(p.Type),
                            Avg = 0,
                            LeftOver = 0,
                            Quantity = 0,
                            ChainActive = false,
                            BranchActive = false,
                            IsManageQuantity = p.IsManageQuantity,
                            MinStorage = p.MinStorage,
                            MaxStorage = p.MaxStorage,
                            CreatedAt = DateTime.UtcNow
                        })
                        .ToList();

                    if (bInventories.Count > 0)
                    {
                        await _context.BInventories.AddRangeAsync(bInventories);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return _mapper.Map<BranchViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(BranchUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.BranchNameRequired);
            }

            if (dto.Name.Length > 100)
            {
                throw new MenuGoException(ErrorCodes.BranchNameLengthExceeded);
            }

            if (dto.OpenTime >= dto.CloseTime)
            {
                throw new MenuGoException(ErrorCodes.BranchOpenCloseTimeInvalid);
            }

            var entity = await _repo.GetByIdAsync(dto.Id);

            if (entity == null)
                return false;

            if (entity.IsDeleted)
            {
                throw new MenuGoException(ErrorCodes.BranchAlreadyDeleted);
            }

            var existingStatus = entity.Status;
            var existingNewWardId = entity.Address?.NewWardId;
            var existingOldWardId = entity.Address?.OldWardId;

            _mapper.Map(dto, entity);

            if (string.IsNullOrWhiteSpace(entity.Status))
            {
                entity.Status = !string.IsNullOrWhiteSpace(existingStatus) ? existingStatus : "Hoạt động";
            }

            if (entity.Address != null)
            {
                entity.Address.Type = "Chi Nhánh";
                if (dto.Address == null || !dto.Address.NewWardId.HasValue || dto.Address.NewWardId == 0)
                {
                    entity.Address.NewWardId = existingNewWardId;
                }
                if (dto.Address == null || !dto.Address.OldWardId.HasValue || dto.Address.OldWardId == 0)
                {
                    entity.Address.OldWardId = existingOldWardId;
                }
            }

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        #region DELETE Xóa mềm chi nhánh
        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.Status = "Ngừng kinh doanh";

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }
        #endregion

        #region DELETE Xóa vĩnh viễn (Hard Delete) toàn bộ dữ liệu của một chi nhánh
        public async Task<bool> HardDeleteBranchAsync(long id)
        {
            if (_context == null)
                throw new InvalidOperationException("DbContext chưa được khởi tạo.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var branch = await _context.Branches.FindAsync(id);
                if (branch == null) return false;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Xóa toàn bộ Chat (Messages, AssignmentHistories, Conversations)
                    var convIds = await _context.Conversations.Where(c => c.BranchId == id).Select(c => c.Id).ToListAsync();
                    if (convIds.Any())
                    {
                        var histories = await _context.ConversationAssignmentHistories.Where(h => convIds.Contains(h.ConversationId)).ToListAsync();
                        _context.ConversationAssignmentHistories.RemoveRange(histories);

                        var messages = await _context.Messages.Where(m => convIds.Contains(m.ConversationId)).ToListAsync();
                        _context.Messages.RemoveRange(messages);

                        var convs = await _context.Conversations.Where(c => convIds.Contains(c.Id)).ToListAsync();
                        _context.Conversations.RemoveRange(convs);
                    }

                    // 2. Xóa Notifications của chi nhánh
                    var notifs = await _context.Notifications.Where(n => n.BranchId == id).ToListAsync();
                    _context.Notifications.RemoveRange(notifs);

                    // 3. Xóa Bàn, Đơn hàng, Thanh toán, Đặt bàn, Khu vực
                    var areaIds = await _context.Areas.Where(a => a.BranchId == id).Select(a => a.Id).ToListAsync();
                    var tableIds = await _context.Tables.Where(t => areaIds.Contains(t.AreaId)).Select(t => t.Id).ToListAsync();

                    var orders = await _context.Orders.Where(o => tableIds.Contains(o.TableId)).ToListAsync();
                    var orderIds = orders.Select(o => o.Id).ToList();

                    if (orderIds.Any())
                    {
                        var payments = await _context.Payments.Where(p => orderIds.Contains(p.OrderId)).ToListAsync();
                        _context.Payments.RemoveRange(payments);

                        var orderDetails = await _context.OrderDetails.Where(od => orderIds.Contains(od.OrderId)).ToListAsync();
                        _context.OrderDetails.RemoveRange(orderDetails);
                    }

                    var reservations = await _context.Reservations.Where(r => r.BranchId == id || (r.OrderId.HasValue && orderIds.Contains(r.OrderId.Value))).ToListAsync();
                    _context.Reservations.RemoveRange(reservations);

                    _context.Orders.RemoveRange(orders);

                    var tables = await _context.Tables.Where(t => tableIds.Contains(t.Id)).ToListAsync();
                    _context.Tables.RemoveRange(tables);

                    var areas = await _context.Areas.Where(a => areaIds.Contains(a.Id)).ToListAsync();
                    _context.Areas.RemoveRange(areas);

                    // 4. Xóa Kho, Sổ kho, Chứng từ kho, Dòng tiền
                    var biIds = await _context.BInventories.Where(bi => bi.BranchId == id).Select(bi => bi.Id).ToListAsync();
                    var docIds = await _context.Documents.Where(d => d.BranchId == id || d.ToBranchId == id).Select(d => d.Id).ToListAsync();

                    if (biIds.Any() || docIds.Any())
                    {
                        var ledgers = await _context.InventoryLedgers
                            .Where(l => (l.DocumentId.HasValue && docIds.Contains(l.DocumentId.Value)) || biIds.Contains(l.BInventoryId))
                            .ToListAsync();
                        _context.InventoryLedgers.RemoveRange(ledgers);
                    }

                    if (docIds.Any())
                    {
                        var docDetails = await _context.DocumentDetails.Where(dd => docIds.Contains(dd.DocumentId)).ToListAsync();
                        _context.DocumentDetails.RemoveRange(docDetails);

                        var docs = await _context.Documents.Where(d => docIds.Contains(d.Id)).ToListAsync();
                        _context.Documents.RemoveRange(docs);
                    }

                    var cashFlows = await _context.CashFlows.Where(cf => cf.BranchId == id).ToListAsync();
                    _context.CashFlows.RemoveRange(cashFlows);

                    var bInventories = await _context.BInventories.Where(bi => bi.BranchId == id).ToListAsync();
                    _context.BInventories.RemoveRange(bInventories);

                    // 5. Xóa Nhân sự, Lương, Lịch làm việc, Hợp đồng, Ticket
                    var payrolls = await _context.Payrolls.Where(p => p.BranchId == id).ToListAsync();
                    var payrollIds = payrolls.Select(p => p.Id).ToList();
                    if (payrollIds.Any())
                    {
                        var salaryDetails = await _context.SalaryDetails.Where(sd => payrollIds.Contains(sd.PayrollId)).ToListAsync();
                        _context.SalaryDetails.RemoveRange(salaryDetails);
                        _context.Payrolls.RemoveRange(payrolls);
                    }

                    var workSchedules = await _context.WorkSchedules.Where(ws => ws.BranchId == id).ToListAsync();
                    _context.WorkSchedules.RemoveRange(workSchedules);

                    var tickets = await _context.Tickets.Where(t => t.BranchId == id).ToListAsync();
                    _context.Tickets.RemoveRange(tickets);

                    var contracts = await _context.Contracts.Where(c => c.BranchId == id).ToListAsync();
                    _context.Contracts.RemoveRange(contracts);

                    // 6. Xóa Thiết bị, Đối tác, Voucher
                    var devices = await _context.Devices.Where(d => d.BranchId == id).ToListAsync();
                    _context.Devices.RemoveRange(devices);

                    var partners = await _context.Partners.Where(p => p.BranchId == id).ToListAsync();
                    _context.Partners.RemoveRange(partners);

                    var vouchers = await _context.Vouchers.Where(v => v.BranchId == id).ToListAsync();
                    _context.Vouchers.RemoveRange(vouchers);

                    // 7. Xóa Chi nhánh & Địa chỉ liên kết (nếu không dùng chung)
                    var addressId = branch.AddressId;
                    _context.Branches.Remove(branch);

                    await _context.SaveChangesAsync();

                    if (addressId > 0)
                    {
                        var isAddressUsedElsewhere = await _context.Branches.AnyAsync(b => b.AddressId == addressId)
                            || await _context.Accounts.AnyAsync(a => a.AddressId == addressId);
                        if (!isAddressUsedElsewhere)
                        {
                            var addr = await _context.Addresses.FindAsync(addressId);
                            if (addr != null)
                            {
                                _context.Addresses.Remove(addr);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        #endregion
    }
}