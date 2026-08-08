using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class WorkScheduleService : IWorkScheduleService
    {
        private readonly IWorkScheduleRepository _repo;
        private readonly IMapper _mapper;
        private readonly IBranchRepository _branchRepo;
        private readonly IShiftRepository _shiftRepo;
        private readonly IAccountRepository _accountRepo;

        public WorkScheduleService(
            IWorkScheduleRepository repo, 
            IMapper mapper,
            IBranchRepository branchRepo,
            IShiftRepository shiftRepo,
            IAccountRepository accountRepo)
        {
            _repo = repo;
            _mapper = mapper;
            _branchRepo = branchRepo;
            _shiftRepo = shiftRepo;
            _accountRepo = accountRepo;
        }

        public async Task<List<WorkScheduleViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<WorkScheduleViewDto>>(data);
        }

        public async Task<WorkScheduleViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<WorkScheduleViewDto>(data);
        }

        public async Task<WorkScheduleViewDto> CreateAsync(WorkScheduleCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (await _branchRepo.GetByIdAsync(dto.BranchId) == null)
            {
                throw new ArgumentException($"Branch with ID {dto.BranchId} does not exist.");
            }
            if (await _shiftRepo.GetByIdAsync(dto.ShiftId) == null)
            {
                throw new ArgumentException($"Shift with ID {dto.ShiftId} does not exist.");
            }
            var account = await _accountRepo.GetByIdAsync(dto.AccountId);
            if (account == null)
            {
                throw new ArgumentException($"Tài khoản với ID {dto.AccountId} không tồn tại.");
            }
            if (account.Contracts == null || !account.Contracts.Any(c => c.BranchId == dto.BranchId && (c.Status == "Active" || c.Status == "1")))
            {
                throw new ArgumentException($"Nhân viên '{account.Name}' (ID: {dto.AccountId}) chưa có hợp đồng làm việc còn hiệu lực tại chi nhánh này.");
            }

            if (await _repo.ExistsAsync(dto.AccountId, dto.ShiftId, dto.WorkDate))
            {
                throw new ArgumentException($"Nhân viên {account.Name} đã được phân công ca làm việc này vào ngày {dto.WorkDate:dd/MM/yyyy}.");
            }

            var shift = await _shiftRepo.GetByIdAsync(dto.ShiftId);
            if (shift != null && await _repo.HasOverlappingScheduleAsync(dto.AccountId, dto.WorkDate, shift.StartTime, shift.EndTime))
            {
                throw new ArgumentException($"Nhân viên {account.Name} đã có ca làm việc trùng giờ trong ngày {dto.WorkDate:dd/MM/yyyy}.");
            }

            var entity = _mapper.Map<WorkSchedule>(dto);
            if (entity.CheckInAt.HasValue && entity.CheckInAt.Value.Kind == DateTimeKind.Unspecified)
            {
                entity.CheckInAt = DateTime.SpecifyKind(entity.CheckInAt.Value, DateTimeKind.Utc);
            }
            if (entity.CheckOutAt.HasValue && entity.CheckOutAt.Value.Kind == DateTimeKind.Unspecified)
            {
                entity.CheckOutAt = DateTime.SpecifyKind(entity.CheckOutAt.Value, DateTimeKind.Utc);
            }

            if (string.IsNullOrWhiteSpace(entity.Code))
            {
                entity.Code = $"WS-{dto.BranchId}-{dto.ShiftId}-{dto.AccountId}-{dto.WorkDate:yyyyMMdd}";
            }
            if (string.IsNullOrWhiteSpace(entity.Status))
            {
                entity.Status = "PENDING";
            }
            entity.CreatedAt = DateTime.UtcNow;

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<WorkScheduleViewDto>(entity);
        }

        public async Task<List<WorkScheduleViewDto>> AssignBulkAsync(WorkScheduleAssignDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (await _branchRepo.GetByIdAsync(dto.BranchId) == null)
            {
                throw new ArgumentException($"Branch with ID {dto.BranchId} does not exist.");
            }
            if (await _shiftRepo.GetByIdAsync(dto.ShiftId) == null)
            {
                throw new ArgumentException($"Shift with ID {dto.ShiftId} does not exist.");
            }

            var validAccountIdsWithContracts = new List<long>();
            foreach (var accountId in dto.AccountIds)
            {
                var acc = await _accountRepo.GetByIdAsync(accountId);
                if (acc != null && acc.Contracts != null && acc.Contracts.Any(c => c.BranchId == dto.BranchId && (c.Status == "Active" || c.Status == "1")))
                {
                    validAccountIdsWithContracts.Add(accountId);
                }
            }

            if (validAccountIdsWithContracts.Count == 0)
            {
                throw new ArgumentException("Không có nhân viên nào trong danh sách được chọn có hợp đồng còn hiệu lực tại chi nhánh này.");
            }

            var createdSchedules = new List<WorkSchedule>();

            foreach (var accountId in validAccountIdsWithContracts)
            {
                foreach (var workDate in dto.WorkDates)
                {
                    if (await _repo.ExistsAsync(accountId, dto.ShiftId, workDate))
                    {
                        continue;
                    }

                    var shift = await _shiftRepo.GetByIdAsync(dto.ShiftId);
                    if (shift != null && await _repo.HasOverlappingScheduleAsync(accountId, workDate, shift.StartTime, shift.EndTime))
                    {
                        continue;
                    }

                    var schedule = new WorkSchedule
                    {
                        AccountId = accountId,
                        BranchId = dto.BranchId,
                        ShiftId = dto.ShiftId,
                        WorkDate = workDate,
                        Code = $"WS-{dto.BranchId}-{dto.ShiftId}-{accountId}-{workDate:yyyyMMdd}",
                        Status = "APPROVED",
                        ManagerApproved = true,
                        CreatedBy = dto.CreatedBy,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _repo.CreateAsync(schedule);
                    createdSchedules.Add(schedule);
                }
            }

            if (createdSchedules.Count > 0)
            {
                await _repo.SaveChangesAsync();
            }

            return _mapper.Map<List<WorkScheduleViewDto>>(createdSchedules);
        }

        public async Task<bool> UpdateAsync(WorkScheduleUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            var account = await _accountRepo.GetByIdAsync(dto.AccountId);
            if (account == null || account.Contracts == null || !account.Contracts.Any(c => c.BranchId == dto.BranchId && (c.Status == "Active" || c.Status == "1")))
            {
                throw new ArgumentException($"Nhân viên này chưa có hợp đồng làm việc còn hiệu lực tại chi nhánh này.");
            }

            _mapper.Map(dto, entity);

            if (entity.CheckInAt.HasValue && entity.CheckInAt.Value.Kind == DateTimeKind.Unspecified)
            {
                entity.CheckInAt = DateTime.SpecifyKind(entity.CheckInAt.Value, DateTimeKind.Utc);
            }
            if (entity.CheckOutAt.HasValue && entity.CheckOutAt.Value.Kind == DateTimeKind.Unspecified)
            {
                entity.CheckOutAt = DateTime.SpecifyKind(entity.CheckOutAt.Value, DateTimeKind.Utc);
            }

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<List<WorkScheduleViewDto>> GetByAccountIdAsync(long accountId)
        {
            var data = await _repo.GetByAccountIdAsync(accountId);
            return _mapper.Map<List<WorkScheduleViewDto>>(data);
        }
        public async Task<(bool Success, string Message, string Action)> RecordAttendanceAsync(long accountId, long branchId)
        {
            var now = DateTime.UtcNow;
            var localNow = now.AddHours(7);
            var today = DateOnly.FromDateTime(localNow);
            var currentTimeOnly = TimeOnly.FromDateTime(localNow);
            
            var schedules = await _repo.GetTodaySchedulesAsync(accountId, branchId, today);
            
            if (schedules == null || !schedules.Any())
            {
                return (false, "Bạn không có ca làm việc hôm nay tại chi nhánh này.", "");
            }

            var workingIndex = schedules.FindIndex(s => s.Status == "Đang làm việc");

            if (workingIndex >= 0)
            {
                // Có ca đang Working -> Tiến hành Check-out
                bool autoSplitOccurred = false;
                
                while (workingIndex >= 0 && workingIndex < schedules.Count - 1)
                {
                    var currentWs = schedules[workingIndex];
                    var nextWs = schedules[workingIndex + 1];
                    
                    // Nếu thời điểm quét thẻ đã vượt qua giờ kết thúc của ca hiện tại một khoảng đáng kể (ví dụ > 30 phút)
                    // Hoặc đơn giản là vượt qua hẳn giờ kết thúc và nhân viên có ca tiếp theo.
                    if (currentTimeOnly >= nextWs.Shift.StartTime && currentTimeOnly >= currentWs.Shift.EndTime)
                    {
                        // Auto-split: Chốt ca hiện tại ở đúng giờ EndTime
                        var localEndTime = today.ToDateTime(currentWs.Shift.EndTime);
                        var utcEndTime = DateTime.SpecifyKind(localEndTime.AddHours(-7), DateTimeKind.Utc);
                        
                        currentWs.CheckOutAt = utcEndTime;
                        var duration1 = currentWs.CheckOutAt.Value - currentWs.CheckInAt.Value;
                        currentWs.ActualHours = (decimal)duration1.TotalHours;
                        currentWs.Status = "Hoàn thành";
                        await _repo.UpdateAsync(currentWs);
                        
                        // Mở ca tiếp theo ở đúng giờ StartTime
                        var localStartTime = today.ToDateTime(nextWs.Shift.StartTime);
                        var utcStartTime = DateTime.SpecifyKind(localStartTime.AddHours(-7), DateTimeKind.Utc);
                        
                        nextWs.CheckInAt = utcStartTime;
                        nextWs.Status = "Đang làm việc";
                        await _repo.UpdateAsync(nextWs);
                        
                        autoSplitOccurred = true;
                        workingIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                // Chốt ca Working cuối cùng với thời gian quét thực tế
                var finalWorking = schedules[workingIndex];
                finalWorking.CheckOutAt = now;
                var duration = finalWorking.CheckOutAt.Value - finalWorking.CheckInAt.Value;
                finalWorking.ActualHours = (decimal)duration.TotalHours;
                finalWorking.Status = "Hoàn thành";
                await _repo.UpdateAsync(finalWorking);
                
                await _repo.SaveChangesAsync();
                
                string msg = autoSplitOccurred 
                    ? "Đã tự động chốt các ca liên tiếp và Check-out thành công" 
                    : "Check-out thành công";
                    
                return (true, msg, "checkout");
            }
            else
            {
                // Không có ca nào đang Working -> Tìm ca đầu tiên chưa check-in và chưa kết thúc
                var nextSchedule = schedules
                    .Where(s => s.CheckInAt == null && (s.Status == "Scheduled" || s.Status == "APPROVED" || s.Status == "PENDING"))
                    .OrderBy(s => s.Shift.StartTime)
                    .FirstOrDefault(s => currentTimeOnly < s.Shift.EndTime);

                if (nextSchedule == null)
                {
                    // Nếu đã qua giờ kết thúc của TẤT CẢ các ca, lấy ca cuối cùng trong ngày
                    nextSchedule = schedules
                        .Where(s => s.CheckInAt == null && (s.Status == "Scheduled" || s.Status == "APPROVED" || s.Status == "PENDING"))
                        .OrderBy(s => s.Shift.StartTime)
                        .LastOrDefault();
                }

                if (nextSchedule != null)
                {
                    // Đánh dấu các ca trước đó là "Absent" (vắng mặt)
                    var missedSchedules = schedules
                        .Where(s => s.CheckInAt == null 
                                 && (s.Status == "Scheduled" || s.Status == "APPROVED" || s.Status == "PENDING")
                                 && s.Id != nextSchedule.Id
                                 && s.Shift.EndTime <= currentTimeOnly)
                        .ToList();

                    foreach (var missed in missedSchedules)
                    {
                        missed.Status = "Vắng mặt";
                        await _repo.UpdateAsync(missed);
                    }

                    nextSchedule.CheckInAt = now;
                    nextSchedule.Status = "Đang làm việc";
                    await _repo.UpdateAsync(nextSchedule);
                    await _repo.SaveChangesAsync();
                    return (true, "Check-in thành công", "checkin");
                }
                else
                {
                    // Nếu tất cả đã Completed, cập nhật lại giờ Check-out cho ca cuối cùng
                    var lastCompleted = schedules.LastOrDefault(s => s.Status == "Hoàn thành");
                    if (lastCompleted != null)
                    {
                        lastCompleted.CheckOutAt = now;
                        var duration = lastCompleted.CheckOutAt.Value - lastCompleted.CheckInAt.Value;
                        lastCompleted.ActualHours = (decimal)duration.TotalHours;
                        await _repo.UpdateAsync(lastCompleted);
                        await _repo.SaveChangesAsync();
                        return (true, "Đã cập nhật giờ Check-out", "checkout");
                    }
                    
                    return (false, "Tất cả ca làm việc hôm nay đã hoàn thành.", "");
                }
            }
        }

        public async Task<List<long>> GetActiveRoleIdsAsync(long accountId)
        {
            return await _accountRepo.GetActiveRoleIdsAsync(accountId);
        }
    }
}

