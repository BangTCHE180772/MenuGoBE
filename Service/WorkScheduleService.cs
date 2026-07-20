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
            var entity = _mapper.Map<WorkSchedule>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<WorkScheduleViewDto>(entity);
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

        public async Task<(bool Success, string Message, string Action)> RecordAttendanceAsync(long accountId, long branchId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            
            var schedule = await _repo.GetTodayScheduleAsync(accountId, branchId, today);
            
            if (schedule == null)
            {
                return (false, "Bạn không có ca làm việc hôm nay tại chi nhánh này.", "");
            }

            if (schedule.CheckInAt == null)
            {
                // Check-in
                schedule.CheckInAt = DateTime.UtcNow;
                schedule.Status = "Working";
                await _repo.UpdateAsync(schedule);
                await _repo.SaveChangesAsync();
                return (true, "Check-in thành công", "checkin");
            }
            else
            {
                // Check-out (or update Check-out if have Check-out before)
                schedule.CheckOutAt = DateTime.UtcNow;
                
                // Calculate ActualHours
                var duration = schedule.CheckOutAt.Value - schedule.CheckInAt.Value;
                schedule.ActualHours = (decimal)duration.TotalHours;
                
                schedule.Status = "Completed";
                
                await _repo.UpdateAsync(schedule);
                await _repo.SaveChangesAsync();
                
                return (true, "Check-out thành công", "checkout");
            }
        }
    }
}
