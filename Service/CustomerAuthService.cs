using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Dtos.Auth;
using MenuGoBE.Helpers;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Microsoft.Extensions.Caching.Memory;
using MenuGoBE.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MenuGoBE.Service
{
    public class CustomerAuthService : ICustomerAuthService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<NotificationHub>? _hubContext;

        public CustomerAuthService(
            ICustomerRepository customerRepo,
            IConfiguration configuration,
            IMapper mapper,
            IEmailService emailService,
            IMemoryCache cache,
            IHubContext<NotificationHub>? hubContext = null)
        {
            _customerRepo = customerRepo;
            _configuration = configuration;
            _mapper = mapper;
            _emailService = emailService;
            _cache = cache;
            _hubContext = hubContext;
        }

        #region CREATE Đăng ký tài khoản khách hàng mới hoặc cập nhật thông tin
        public async Task<bool> RegisterAsync(CustomerRegisterDto dto)
        {
            var phone = dto.Phone.Trim();
            var customer = await _customerRepo.GetByPhoneAsync(phone);

            if (customer != null)
            {
                if (!string.IsNullOrEmpty(customer.HashedPassword))
                {
                    throw new InvalidOperationException("Số điện thoại này đã được đăng ký.");
                }

                // Cashier created this profile earlier. We enrich it.
                customer.HashedPassword = HashHelper.HashPassword(dto.Password);
                customer.Email = dto.Email?.Trim().ToLower();
                if (!string.IsNullOrEmpty(dto.Name))
                {
                    customer.Name = dto.Name.Trim();
                }

                await _customerRepo.UpdateAsync(customer);
            }
            else
            {
                // Create new customer
                customer = new Customer
                {
                    Phone = phone,
                    Name = dto.Name.Trim(),
                    HashedPassword = HashHelper.HashPassword(dto.Password),
                    Email = dto.Email?.Trim().ToLower(),
                    Point = 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _customerRepo.CreateAsync(customer);
            }

            await _customerRepo.SaveChangesAsync();

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveCustomerUpdate", new { action = "register", customerId = customer.Id });
                }
                catch { }
            }

            return true;
        }
        #endregion

        #region GET Đăng nhập tài khoản khách hàng và cấp token
        public async Task<CustomerLoginResponseDto> LoginAsync(CustomerLoginDto dto)
        {
            var phone = dto.Phone.Trim();
            var customer = await _customerRepo.GetByPhoneAsync(phone);

            if (customer == null || string.IsNullOrEmpty(customer.HashedPassword))
            {
                throw new KeyNotFoundException("Tài khoản chưa được đăng ký. Vui lòng đăng ký tài khoản trước.");
            }

            bool isPasswordValid = HashHelper.VerifyPassword(dto.Password, customer.HashedPassword);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Số điện thoại hoặc mật khẩu không chính xác.");
            }

            var token = GenerateCustomerToken(customer);

            return new CustomerLoginResponseDto
            {
                Token = token,
                Customer = _mapper.Map<CustomerProfileDto>(customer)
            };
        }
        #endregion

        #region UTILS Tạo JSON Web Token cho khách hàng
        private string GenerateCustomerToken(Customer customer)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
                new Claim("CustomerId", customer.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                new Claim(ClaimTypes.Role, "Customer"),
                new Claim("role", "Customer"),
                new Claim("name", customer.Name),
                new Claim("phone", customer.Phone)
            };

            if (!string.IsNullOrEmpty(customer.Email))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, customer.Email));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion

        #region POST Gửi mã OTP khôi phục mật khẩu qua Email
        public async Task<bool> SendResetPasswordOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Vui lòng nhập địa chỉ email.");
            }

            var normalizedEmail = email.Trim().ToLower();
            var customer = await _customerRepo.GetByEmailAsync(normalizedEmail);
            if (customer == null)
            {
                throw new KeyNotFoundException("Không tìm thấy tài khoản khách hàng gắn với địa chỉ email này.");
            }

            // Sinh mã OTP 6 chữ số
            var otp = new Random().Next(100000, 999999).ToString();
            _cache.Set($"OTP_CUSTOMER_RESET_{normalizedEmail}", otp, TimeSpan.FromMinutes(5));

            var subject = "Mã xác nhận đặt lại mật khẩu - MenuGo";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #f1f5f9; border-radius: 12px;'>
                    <h2 style='color: #ea580c; margin-top: 0;'>MenuGo - Khôi phục mật khẩu</h2>
                    <p>Xin chào <strong>{customer.Name}</strong>,</p>
                    <p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản khách hàng của bạn.</p>
                    <p>Mã xác nhận (OTP) của bạn là:</p>
                    <div style='background: #fff7ed; border: 1px dashed #ea580c; border-radius: 8px; padding: 14px; text-align: center; margin: 20px 0;'>
                        <span style='font-size: 28px; font-weight: 800; letter-spacing: 6px; color: #ea580c;'>{otp}</span>
                    </div>
                    <p style='color: #64748b; font-size: 13px;'>Mã xác nhận này có hiệu lực trong vòng <strong>5 phút</strong>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                    <hr style='border: none; border-top: 1px solid #f1f5f9; margin: 20px 0;' />
                    <p style='color: #94a3b8; font-size: 12px;'>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                </div>";

            await _emailService.SendEmailAsync(normalizedEmail, subject, body);
            return true;
        }
        #endregion

        #region POST Xác nhận OTP và đặt lại mật khẩu mới
        public async Task<bool> ResetPasswordWithOtpAsync(CustomerResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.OtpCode) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                throw new ArgumentException("Vui lòng điền đầy đủ email, mã xác nhận và mật khẩu mới.");
            }

            var normalizedEmail = dto.Email.Trim().ToLower();
            if (!_cache.TryGetValue($"OTP_CUSTOMER_RESET_{normalizedEmail}", out string? cachedOtp) || cachedOtp != dto.OtpCode.Trim())
            {
                throw new InvalidOperationException("Mã xác nhận không chính xác hoặc đã hết hạn (5 phút).");
            }

            var customer = await _customerRepo.GetByEmailAsync(normalizedEmail);
            if (customer == null)
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin tài khoản khách hàng.");
            }

            customer.HashedPassword = HashHelper.HashPassword(dto.NewPassword);
            await _customerRepo.UpdateAsync(customer);
            await _customerRepo.SaveChangesAsync();

            // Xóa cache sau khi dùng
            _cache.Remove($"OTP_CUSTOMER_RESET_{normalizedEmail}");
            return true;
        }
        #endregion
    }
}
