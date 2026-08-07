using Microsoft.EntityFrameworkCore;
using MenuGoBE.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Service;
using MenuGoBE.Service.Document;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Repositories;
using MenuGoBE.Repositories.Document;
using MenuGoBE.Controllers.Document;
using MenuGoBE.Mapper;
using MenuGoBE.Exceptions;
using AutoMapper;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using MenuGoBE.Models;
using MenuGoBE.Helpers;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PayOS;


var builder = WebApplication.CreateBuilder(args);

// System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = "name"
        };
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfiles));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Repository
builder.Services.AddScoped<IChainRepository, ChainRepository>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<INewWardRepository, NewWardRepository>();
builder.Services.AddScoped<IOldWardRepository, OldWardRepository>();
builder.Services.AddScoped<INewProvinceRepository, NewProvinceRepository>();
builder.Services.AddScoped<IOldProvinceRepository, OldProvinceRepository>();
builder.Services.AddScoped<IOldDistrictRepository, OldDistrictRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<ISalaryDetailRepository, SalaryDetailRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IBInventoryRepository, BInventoryRepository>();
builder.Services.AddScoped<IRecipesDetailedRepository, RecipesDetailedRepository>();
builder.Services.AddScoped<IBInventoryRepository, BInventoryRepository>();

// Service

builder.Services.AddScoped<IChainService, ChainService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderDetailService, OrderDetailService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkScheduleService, WorkScheduleService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IProductService, ProductService>();
// builder.Services.AddScoped<IProductionDocumentService, ProductionDocumentService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IEmailService,EmailService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IKitchenService, KitchenService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddHostedService<AutoCheckoutService>();
builder.Services.AddScoped<INewWardService, NewWardService>();
builder.Services.AddScoped<IOldWardService, OldWardService>();
builder.Services.AddScoped<INewProvinceService, NewProvinceService>();
builder.Services.AddScoped<IOldProvinceService, OldProvinceService>();
builder.Services.AddScoped<IOldDistrictService, OldDistrictService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<ISalaryDetailService, SalaryDetailService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IEmailService,EmailService>();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IDictionary<string, string>>(opts => new Dictionary<string, string>());
builder.Services.AddSingleton<IDictionary<string, long>>(opts => new Dictionary<string, long>());


// Signal & Monitor Services for Reservation
builder.Services.AddSingleton<IReservationSignalService, ReservationSignalService>();
builder.Services.AddHostedService<ReservationMonitorService>();

// Device Auth Service (QR + SignalR)
builder.Services.AddSingleton<DeviceAuthService>();
builder.Services.AddSingleton<IDeviceAuthService>(provider => provider.GetRequiredService<DeviceAuthService>());
builder.Services.AddHostedService(provider => provider.GetRequiredService<DeviceAuthService>());

// PayOS setup
var payOsConfig = builder.Configuration.GetSection("PayOS");
builder.Services.AddSingleton(new PayOSClient(
    payOsConfig["ClientId"] ?? string.Empty,
    payOsConfig["ApiKey"] ?? string.Empty,
    payOsConfig["ChecksumKey"] ?? string.Empty
));

// Payment Service
builder.Services.AddScoped<IPaymentService, PaymentService>();


builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers().AddOData(options =>
{
    var odataBuilder = new ODataConventionModelBuilder();
    odataBuilder.EntitySet<Customer>("CustomerOData");
    odataBuilder.EntitySet<Product>("ProductOData");
    options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100)
           .AddRouteComponents("odata", odataBuilder.GetEdmModel());
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // REMOVED to fix CORS / Token stripping on redirect

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MenuGoBE.Hubs.NotificationHub>("/notificationHub");

app.Run();
