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
                bool autoSplitOccurred = false;
                
                while (workingIndex >= 0 && workingIndex < schedules.Count - 1)
                {
                    var currentWs = schedules[workingIndex];
                    var nextWs = schedules[workingIndex + 1];
                    
                    if (currentTimeOnly >= nextWs.Shift.StartTime && currentTimeOnly > currentWs.Shift.EndTime)
                    {
                        var localEndTime = today.ToDateTime(currentWs.Shift.EndTime);
                        var utcEndTime = localEndTime.AddHours(-7);
                        
                        currentWs.CheckOutAt = utcEndTime;
                        var duration1 = currentWs.CheckOutAt.Value - currentWs.CheckInAt.Value;
                        currentWs.ActualHours = (decimal)duration1.TotalHours;
                        currentWs.Status = "Completed";
                        await _repo.UpdateAsync(currentWs);
                        
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
                var nextSchedule = schedules.FirstOrDefault(s => s.Status == "Scheduled" || s.CheckInAt == null);
                if (nextSchedule != null)
                {
                    nextSchedule.CheckInAt = now;
                    nextSchedule.Status = "Working";
                    await _repo.UpdateAsync(nextSchedule);
                    await _repo.SaveChangesAsync();
                    return (true, "Check-in thành công", "checkin");
                }
                else
                {
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

