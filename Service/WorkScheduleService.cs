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

        public WorkScheduleService(IWorkScheduleRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
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

            if (!await _repo.BranchExistsAsync(dto.BranchId))
            {
                throw new ArgumentException($"Branch with ID {dto.BranchId} does not exist.");
            }
            if (!await _repo.ShiftExistsAsync(dto.ShiftId))
            {
                throw new ArgumentException($"Shift with ID {dto.ShiftId} does not exist.");
            }
            var validAccounts = await _repo.GetValidAccountIdsAsync(new List<long> { dto.AccountId });
            if (validAccounts.Count == 0)
            {
                throw new ArgumentException($"Account with ID {dto.AccountId} does not exist.");
            }

            if (await _repo.ExistsAsync(dto.AccountId, dto.ShiftId, dto.WorkDate))
            {
                throw new ArgumentException($"Account with ID {dto.AccountId} is already assigned to shift {dto.ShiftId} on {dto.WorkDate}.");
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

            if (!await _repo.BranchExistsAsync(dto.BranchId))
            {
                throw new ArgumentException($"Branch with ID {dto.BranchId} does not exist.");
            }
            if (!await _repo.ShiftExistsAsync(dto.ShiftId))
            {
                throw new ArgumentException($"Shift with ID {dto.ShiftId} does not exist.");
            }

            var validAccountIds = await _repo.GetValidAccountIdsAsync(dto.AccountIds);
            if (validAccountIds.Count == 0)
            {
                return new List<WorkScheduleViewDto>();
            }

            var createdSchedules = new List<WorkSchedule>();

            foreach (var accountId in validAccountIds)
            {
                foreach (var workDate in dto.WorkDates)
                {
                    if (await _repo.ExistsAsync(accountId, dto.ShiftId, workDate))
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

            var workingIndex = schedules.FindIndex(s => s.Status == "Working");

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
                        var utcEndTime = localEndTime.AddHours(-7);
                        
                        currentWs.CheckOutAt = utcEndTime;
                        var duration1 = currentWs.CheckOutAt.Value - currentWs.CheckInAt.Value;
                        currentWs.ActualHours = (decimal)duration1.TotalHours;
                        currentWs.Status = "Completed";
                        await _repo.UpdateAsync(currentWs);
                        
                        // Mở ca tiếp theo ở đúng giờ StartTime
                        var localStartTime = today.ToDateTime(nextWs.Shift.StartTime);
                        var utcStartTime = localStartTime.AddHours(-7);
                        
                        nextWs.CheckInAt = utcStartTime;
                        nextWs.Status = "Working";
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
                finalWorking.Status = "Completed";
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
                        missed.Status = "Absent";
                        await _repo.UpdateAsync(missed);
                    }

                    nextSchedule.CheckInAt = now;
                    nextSchedule.Status = "Working";
                    await _repo.UpdateAsync(nextSchedule);
                    await _repo.SaveChangesAsync();
                    return (true, "Check-in thành công", "checkin");
                }
                else
                {
                    // Nếu tất cả đã Completed, cập nhật lại giờ Check-out cho ca cuối cùng
                    var lastCompleted = schedules.LastOrDefault(s => s.Status == "Completed");
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
            return await _repo.GetActiveRoleIdsAsync(accountId);
        }
    }
}

