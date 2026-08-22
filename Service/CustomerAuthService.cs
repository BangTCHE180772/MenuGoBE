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

namespace MenuGoBE.Service
{
    public class CustomerAuthService : ICustomerAuthService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public CustomerAuthService(
            ICustomerRepository customerRepo,
            IConfiguration configuration,
            IMapper mapper)
        {
            _customerRepo = customerRepo;
            _configuration = configuration;
            _mapper = mapper;
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
    }
}
