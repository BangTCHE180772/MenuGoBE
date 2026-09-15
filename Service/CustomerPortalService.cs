using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Auth;
using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Helpers;
using MenuGoBE.Hubs;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class CustomerPortalService : ICustomerPortalService
    {
        private readonly AppDbContext _context;
        private readonly ICustomerRepository _customerRepo;
        private readonly IReservationService _reservationService;
        private readonly IOrderRepository _orderRepo;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IMapper _mapper;

        public CustomerPortalService(
            AppDbContext context,
            ICustomerRepository customerRepo,
            IReservationService reservationService,
            IOrderRepository orderRepo,
            IHubContext<NotificationHub> hubContext,
            IMapper mapper)
        {
            _context = context;
            _customerRepo = customerRepo;
            _reservationService = reservationService;
            _orderRepo = orderRepo;
            _hubContext = hubContext;
            _mapper = mapper;
        }

        #region GET Lấy thông tin hồ sơ khách hàng
        public async Task<CustomerProfileDto> GetProfileAsync(long customerId)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null) throw new KeyNotFoundException("Khách hàng không tồn tại.");

            return _mapper.Map<CustomerProfileDto>(customer);
        }
        #endregion

        #region PUT Cập nhật thông tin hồ sơ và đổi mật khẩu khách hàng
        public async Task<CustomerProfileDto> UpdateProfileAsync(long customerId, CustomerUpdateProfileDto dto)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null) throw new KeyNotFoundException("Khách hàng không tồn tại.");

            var newPhone = dto.Phone?.Trim();
            if (!string.IsNullOrEmpty(newPhone) && newPhone != customer.Phone)
            {
                var existingCustomer = await _customerRepo.GetByPhoneAsync(newPhone);
                if (existingCustomer != null && existingCustomer.Id != customerId)
                {
                    throw new InvalidOperationException("Số điện thoại này đã được sử dụng bởi một tài khoản khác.");
                }
                customer.Phone = newPhone;
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                customer.Name = dto.Name.Trim();
            }

            if (dto.Email != null)
            {
                var newEmail = dto.Email.Trim().ToLower();
                if (!string.IsNullOrEmpty(newEmail) && newEmail != customer.Email)
                {
                    var existingEmail = await _customerRepo.GetByEmailAsync(newEmail);
                    if (existingEmail != null && existingEmail.Id != customerId)
                    {
                        throw new InvalidOperationException("Email này đã được sử dụng bởi một tài khoản khác.");
                    }
                    customer.Email = newEmail;
                }
                else if (string.IsNullOrEmpty(newEmail))
                {
                    customer.Email = null;
                }
            }

            // Đổi mật khẩu nếu có yêu cầu
            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                if (!string.IsNullOrEmpty(customer.HashedPassword))
                {
                    if (string.IsNullOrEmpty(dto.CurrentPassword) || !HashHelper.VerifyPassword(dto.CurrentPassword, customer.HashedPassword))
                    {
                        throw new InvalidOperationException("Mật khẩu hiện tại không chính xác.");
                    }
                }

                customer.HashedPassword = HashHelper.HashPassword(dto.NewPassword);
            }

            customer.ReceivePromoEmails = dto.ReceivePromoEmails;

            // Đồng bộ thông tin cập nhật sang bảng Partner (ở tất cả các chi nhánh)
            var linkedPartners = await _context.Partners.Where(p => p.CustomerId == customer.Id).ToListAsync();
            foreach (var partner in linkedPartners)
            {
                partner.Name = customer.Name;
                partner.Phone = customer.Phone;
                if (customer.Email != null)
                {
                    partner.Email = customer.Email;
                }
            }

            await _customerRepo.UpdateAsync(customer);
            await _context.SaveChangesAsync();

            return _mapper.Map<CustomerProfileDto>(customer);
        }
        #endregion

        #region GET Lấy danh sách lịch đặt bàn của khách hàng
        public async Task<List<ReservationViewDto>> GetMyReservationsAsync(long customerId)
        {
            var reservations = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Branch)
                .Include(r => r.Order)
                    .ThenInclude(o => o!.Table)
                        .ThenInclude(t => t!.Area)
                            .ThenInclude(a => a!.Branch)
                .Include(r => r.Order)
                    .ThenInclude(o => o!.OrderDetails)
                        .ThenInclude(od => od.Product)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var dtos = new List<ReservationViewDto>();
            foreach (var r in reservations)
            {
                var dto = new ReservationViewDto
                {
                    Id = r.Id,
                    CustomerId = r.CustomerId,
                    CustomerName = r.Customer?.Name ?? "",
                    CustomerPhone = r.Customer?.Phone ?? "",
                    ReservationTime = r.ReservationTime,
                    NumberOfGuests = r.NumberOfGuests,
                    Note = r.Note,
                    Status = r.Status,
                    OrderId = r.OrderId,
                    BranchId = r.BranchId ?? r.Order?.Table?.Area?.BranchId,
                    BranchName = r.Branch?.Name ?? r.Order?.Table?.Area?.Branch?.Name ?? "",
                    CreatedAt = r.CreatedAt
                };

                if (r.OrderId.HasValue && r.Order != null)
                {
                    dto.TableNames.Add(r.Order.Table?.Name ?? "");
                    var preOrderDetails = r.Order.OrderDetails
                        .Where(od => od.Status == "PreOrder" || od.CookingStatus == "PreOrder")
                        .ToList();

                    dto.PreOrderItemsCount = preOrderDetails.Count;
                    dto.PreOrderItems = preOrderDetails.Select(od => new PreOrderItemDto
                    {
                        ProductId = od.ProductId,
                        ProductName = od.Product?.Name ?? "Món ăn",
                        Quantity = od.Quantity,
                        Price = od.Price,
                        Note = od.Note ?? ""
                    }).ToList();

                    var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(r.OrderId.Value);
                    foreach (var child in childOrders)
                    {
                        dto.TableNames.Add(child.Table?.Name ?? "");
                    }
                }
                dtos.Add(dto);
            }

            return dtos;
        }
        #endregion

        #region CREATE Tạo mới yêu cầu đặt bàn của khách hàng
        public async Task<object> CreateReservationAsync(long customerId, ReservationCreateDto dto)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null) throw new KeyNotFoundException("Khách hàng không tồn tại.");

            // Overwrite customer info from token to prevent spoofing
            dto.CustomerName = customer.Name;
            dto.CustomerPhone = customer.Phone;
            dto.ConfirmUpdateCustomer = true; // Always true to skip name conflict exception

            var result = await _reservationService.CreateAsync(dto);

            // Send SignalR notification to the branch
            if (dto.BranchId.HasValue)
            {
                await _hubContext.Clients.Group($"branch_{dto.BranchId.Value}").SendAsync("ReceiveNotification", new
                {
                    type = "new-reservation",
                    message = $"Khách hàng {customer.Name} đã gửi một yêu cầu đặt bàn mới.",
                    timestamp = DateTime.UtcNow
                });
            }

            return result;
        }
        #endregion

        #region UPDATE Hủy lịch đặt bàn của khách hàng
        public async Task<bool> CancelReservationAsync(long customerId, long reservationId)
        {
            var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && r.CustomerId == customerId);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy lịch đặt bàn.");

            if (reservation.Status != "Pending" && reservation.Status != "Confirmed")
            {
                throw new InvalidOperationException("Chỉ có thể hủy lịch đặt bàn ở trạng thái Chờ xác nhận hoặc Đã xác nhận.");
            }

            var success = await _reservationService.CancelAsync(reservationId);
            return success;
        }
        #endregion

        #region GET Lấy danh sách thông báo của khách hàng
        public async Task<List<Notification>> GetMyNotificationsAsync(long customerId)
        {
            return await _context.Notifications
                .Where(n => n.CustomerId == customerId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        #endregion
    }
}
