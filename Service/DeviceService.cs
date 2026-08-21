using MenuGoBE.Data;
using MenuGoBE.Dtos.Device;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _repo;
        private readonly AppDbContext _context;

        public DeviceService(IDeviceRepository repo, AppDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<object> SetupAsync(DeviceSetupDto dto, long branchId, long accountId)
        {
            var existingDevice = await _context.Devices.FirstOrDefaultAsync(d => d.BranchId == branchId && d.Name.ToLower() == dto.Name.ToLower() && d.IsActive);
            if (existingDevice != null)
                throw new Exception("Tên thiết bị đã tồn tại và đang hoạt động trong chi nhánh. Vui lòng chọn tên khác.");

            var device = new Device
            {
                BranchId = branchId,
                Name = dto.Name,
                DeviceType = dto.DeviceType,
                Token = Guid.NewGuid().ToString(),
                IsActive = true,
                SetupBy = accountId,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(device);

            return new
            {
                deviceToken = device.Token,
                deviceInfo = new
                {
                    id = device.Id,
                    branchId = device.BranchId,
                    name = device.Name,
                    deviceType = device.DeviceType,
                    isActive = device.IsActive
                }
            };
        }

        public async Task<object?> ValidateAsync(DeviceValidateDto dto)
        {
            var device = await _repo.GetByTokenAsync(dto.Token);

            if (device == null)
                throw new Exception("Thiết bị không tồn tại.");

            if (!device.IsActive)
                throw new Exception("Thiết bị đã bị thu hồi quyền. Vui lòng liên hệ quản lý.");

            device.LastActiveAt = DateTime.UtcNow;
            await _repo.UpdateAsync(device);

            return new
            {
                id = device.Id,
                branchId = device.BranchId,
                branchName = device.Branch?.Name,
                name = device.Name,
                deviceType = device.DeviceType,
                isActive = device.IsActive
            };
        }

        public async Task<List<object>> GetByBranchIdAsync(long branchId)
        {
            var devices = await _repo.GetByBranchIdAsync(branchId);
            return devices.Select(d => new
            {
                d.Id,
                d.BranchId,
                d.Name,
                d.DeviceType,
                d.IsActive,
                d.SetupBy,
                d.CreatedAt,
                d.LastActiveAt
            }).Cast<object>().ToList();
        }

        public async Task UpdateAsync(DeviceUpdateDto dto)
        {
            var device = await _repo.GetByIdAsync(dto.Id);
            if (device == null)
                throw new Exception("Không tìm thấy thiết bị.");

            if (dto.IsActive)
            {
                var existingDevice = await _context.Devices.FirstOrDefaultAsync(d => d.BranchId == device.BranchId && d.Id != device.Id && d.Name.ToLower() == dto.Name.ToLower() && d.IsActive);
                if (existingDevice != null)
                    throw new Exception("Tên thiết bị đã tồn tại và đang hoạt động. Vui lòng chọn tên khác trước khi cập nhật hoặc kích hoạt.");
            }

            device.Name = dto.Name;
            device.DeviceType = dto.DeviceType;
            device.IsActive = dto.IsActive;

            await _repo.UpdateAsync(device);
        }

        public async Task DeleteAsync(long id)
        {
            var device = await _repo.GetByIdAsync(id);
            if (device == null)
                throw new Exception("Không tìm thấy thiết bị.");

            await _repo.DeleteAsync(id);
        }

        public async Task RevokeAsync(long id)
        {
            var device = await _repo.GetByIdAsync(id);
            if (device == null)
                throw new Exception("Không tìm thấy thiết bị.");

            device.IsActive = false;
            await _repo.UpdateAsync(device);
        }

        public async Task<object?> EmployeeLoginAsync(DeviceEmployeeLoginDto dto)
        {
            var device = await _repo.GetByTokenAsync(dto.DeviceToken);

            if (device == null || !device.IsActive)
                throw new Exception("Thiết bị không hợp lệ hoặc đã bị thu hồi.");

            var account = await _context.Accounts
                .Include(a => a.Contracts)
                    .ThenInclude(c => c.Role)
                .FirstOrDefaultAsync(a => a.Email == dto.Email && a.IsActive);

            if (account == null)
                throw new Exception("Email không tồn tại hoặc tài khoản bị khóa.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var hasValidContract = account.Contracts.Any(c =>
                c.BranchId == device.BranchId &&
                c.Status == "Active" &&
                c.StartDate <= today &&
                (c.EndDate == null || c.EndDate >= today));

            if (!hasValidContract)
                throw new Exception("Nhân viên không thuộc chi nhánh của thiết bị này.");



            var roles = account.Contracts
                .Where(c => c.BranchId == device.BranchId && c.Status == "Active" &&
                            c.StartDate <= today && (c.EndDate == null || c.EndDate >= today))
                .Select(c => c.Role.Name)
                .Distinct()
                .ToList();

            bool isManagerOrAbove = roles.Any(r => r == "Manager" || r == "Owner" || r == "Admin");

            DateTime? shiftEndTime = null;
            DateTime? disconnectTime = null;

            if (!isManagerOrAbove)
            {
                if (device.DeviceType == "Station" && !roles.Contains("Waiter"))
                    throw new Exception("Vui lòng vào đúng vị trí làm việc (Thiết bị này dành cho Phục vụ).");
                if (device.DeviceType == "POS" && !roles.Contains("Cashier"))
                    throw new Exception("Vui lòng vào đúng vị trí làm việc (Thiết bị này dành cho Thu ngân).");
                if (device.DeviceType == "Kitchen" && !roles.Contains("Chef"))
                    throw new Exception("Vui lòng vào đúng vị trí làm việc (Thiết bị này dành cho Bếp).");

                var activeSchedules = await _context.WorkSchedules
                    .Include(ws => ws.Shift)
                    .Where(ws => ws.AccountId == account.Id
                              && ws.BranchId == device.BranchId
                              && ws.WorkDate == today
                              && ws.CheckInAt != null)
                    .ToListAsync();

                if (!activeSchedules.Any())
                    throw new Exception("Bạn chưa check-in ca làm việc hôm nay tại chi nhánh này.");

                var nowDateTime = DateTime.UtcNow.AddHours(7);
                var todayDt = nowDateTime.Date;

                var validSchedule = activeSchedules.FirstOrDefault(s => {
                    var shiftEndDateTime = todayDt.Add(s.Shift.EndTime.ToTimeSpan());
                    return shiftEndDateTime.AddMinutes(30) >= nowDateTime;
                });

                if (validSchedule == null)
                    throw new Exception("Ca làm việc của bạn đã kết thúc quá 30 phút hoặc chưa tới ca.");

                shiftEndTime = todayDt.Add(validSchedule.Shift.EndTime.ToTimeSpan());
                disconnectTime = shiftEndTime.Value.AddMinutes(30);
            }

            device.LastActiveAt = DateTime.UtcNow;
            await _repo.UpdateAsync(device);

            return new
            {
                success = true,
                employee = new
                {
                    id = account.Id,
                    name = account.Name,
                    email = account.Email,
                    roles = roles,
                    shiftEndTime = shiftEndTime,
                    disconnectTime = disconnectTime
                }
            };
        }

        public async Task<Device?> GetDeviceByTokenAsync(string token)
        {
            return await _context.Devices.FirstOrDefaultAsync(d => d.Token == token);
        }

        public async Task<object?> PersonalLoginAsync(string deviceToken, string email)
        {
            var device = await _repo.GetByTokenAsync(deviceToken);
            if (device == null || !device.IsActive)
                throw new Exception("Thiết bị không hợp lệ hoặc đã bị thu hồi.");

            var account = await _context.Accounts
                .Include(a => a.Contracts)
                    .ThenInclude(c => c.Role)
                .FirstOrDefaultAsync(a => a.Email == email && a.IsActive);

            if (account == null)
                throw new Exception("Email không tồn tại hoặc tài khoản bị khóa.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var hasValidContract = account.Contracts.Any(c =>
                c.BranchId == device.BranchId &&
                c.Status == "Active" &&
                c.StartDate <= today &&
                (c.EndDate == null || c.EndDate >= today));

            if (!hasValidContract)
                throw new Exception("Nhân viên không thuộc chi nhánh của thiết bị này.");

            var roles = account.Contracts
                .Where(c => c.BranchId == device.BranchId && c.Status == "Active" &&
                            c.StartDate <= today && (c.EndDate == null || c.EndDate >= today))
                .Select(c => c.Role.Name)
                .Distinct()
                .ToList();

            bool isManagerOrAbove = roles.Any(r => r == "Manager" || r == "Owner" || r == "Admin");

            DateTime? shiftEndTime = null;
            DateTime? disconnectTime = null;

            if (!isManagerOrAbove)
            {
                if (device.DeviceType == "Station" && !roles.Contains("Waiter"))
                    throw new Exception("Vui lòng vào đúng vị trí làm việc (Thiết bị này dành cho Phục vụ).");
                if (device.DeviceType == "POS" && !roles.Contains("Cashier"))
                    throw new Exception("Vui lòng vào đúng vị trí làm việc (Thiết bị này dành cho Thu ngân).");
                if (device.DeviceType == "Kitchen" && !roles.Contains("Chef"))
                    throw new Exception("Vui lòng vào đúng vị trí làm việc (Thiết bị này dành cho Bếp).");

                var activeSchedules = await _context.WorkSchedules
                    .Include(ws => ws.Shift)
                    .Where(ws => ws.AccountId == account.Id
                              && ws.BranchId == device.BranchId
                              && ws.WorkDate == today
                              && ws.CheckInAt != null)
                    .ToListAsync();

                if (!activeSchedules.Any())
                    throw new Exception("Bạn chưa check-in ca làm việc hôm nay tại chi nhánh này.");

                var nowDateTime = DateTime.UtcNow.AddHours(7);
                var todayDt = nowDateTime.Date;

                var validSchedule = activeSchedules.FirstOrDefault(s => {
                    var shiftEndDateTime = todayDt.Add(s.Shift.EndTime.ToTimeSpan());
                    return shiftEndDateTime.AddMinutes(30) >= nowDateTime;
                });

                if (validSchedule == null)
                    throw new Exception("Ca làm việc của bạn đã kết thúc quá 30 phút hoặc chưa tới ca.");

                shiftEndTime = todayDt.Add(validSchedule.Shift.EndTime.ToTimeSpan());
                disconnectTime = shiftEndTime.Value.AddMinutes(30);
            }

            device.LastActiveAt = DateTime.UtcNow;
            await _repo.UpdateAsync(device);

            return new
            {
                success = true,
                employee = new
                {
                    id = account.Id,
                    name = account.Name,
                    email = account.Email,
                    roles = roles,
                    shiftEndTime = shiftEndTime,
                    disconnectTime = disconnectTime
                }
            };
        }
    }
}
