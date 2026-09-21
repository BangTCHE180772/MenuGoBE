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
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

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
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});


builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfiles));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddMemoryCache();

builder.Services.Configure<PointSystemConfig>(builder.Configuration.GetSection("PointSystem"));


//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Repository
builder.Services.AddScoped<IChainRepository, ChainRepository>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();
builder.Services.AddScoped<ILeftoverRecordRepository, LeftoverRecordRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IShiftChangeRequestRepository, ShiftChangeRequestRepository>();
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
builder.Services.AddScoped<ICashFlowRepository, CashFlowRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IBInventoryRepository, BInventoryRepository>();
builder.Services.AddScoped<IRecipesDetailedRepository, RecipesDetailedRepository>();
builder.Services.AddScoped<IBInventoryRepository, BInventoryRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IOrderAssignmentRepository, OrderAssignmentRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IShiftFeedbackRepository, ShiftFeedbackRepository>();
builder.Services.AddScoped<IPayrollSuggestionRepository, PayrollSuggestionRepository>();

// Service
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IChainService, ChainService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<ILeftoverRecordService, LeftoverRecordService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderDetailService, OrderDetailService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkScheduleService, WorkScheduleService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IShiftChangeRequestService, ShiftChangeRequestService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IFefoAllocationService, FefoAllocationService>();
// builder.Services.AddScoped<IProductionDocumentService, ProductionDocumentService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICustomerAuthService, CustomerAuthService>();
builder.Services.AddScoped<ICustomerPortalService, CustomerPortalService>();
builder.Services.AddScoped<IChatService, ChatService>();
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
builder.Services.AddHostedService<MenuGoBE.Service.BackgroundService.NotificationSyncWorker>();
builder.Services.AddScoped<IOrderAssignmentService, OrderAssignmentService>();
builder.Services.AddHostedService<OrderAssignmentMonitorService>();
builder.Services.AddHostedService<GuestChatCleanupService>();
builder.Services.AddScoped<INewWardService, NewWardService>();
builder.Services.AddScoped<IOldWardService, OldWardService>();
builder.Services.AddScoped<INewProvinceService, NewProvinceService>();
builder.Services.AddScoped<IOldProvinceService, OldProvinceService>();
builder.Services.AddScoped<IOldDistrictService, OldDistrictService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<ISalaryDetailService, SalaryDetailService>();
builder.Services.AddScoped<IHolidayConfigService, HolidayConfigService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICashFlowService, CashFlowService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IEmailService,EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IShiftFeedbackService, ShiftFeedbackService>();
builder.Services.AddScoped<IPayrollSuggestionService, PayrollSuggestionService>();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IDictionary<string, string>>(opts => new Dictionary<string, string>());
builder.Services.AddSingleton<IDictionary<string, long>>(opts => new Dictionary<string, long>());


// Signal & Monitor Services for Reservation
builder.Services.AddSingleton<IReservationSignalService, ReservationSignalService>();
builder.Services.AddHostedService<ReservationMonitorService>();

// Reservation Email Notification Queue & Worker
builder.Services.AddSingleton<IReservationNotificationQueue, ReservationNotificationQueue>();
builder.Services.AddHostedService<ReservationNotificationWorker>();

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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Thao tác quá nhanh. Vui lòng thử lại sau." }, token);
    };

    options.AddPolicy("CustomerActionPolicy", httpContext =>
    {
        var user = httpContext.User;
        bool isStaff = user.Identity != null && user.Identity.IsAuthenticated && 
                       user.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role && 
                                    (c.Value == "Admin" || c.Value == "Cashier" || c.Value == "Waiter"));
        if (isStaff)
        {
            return RateLimitPartition.GetNoLimiter("NoLimit");
        }

        var tableId = httpContext.Request.RouteValues["tableId"]?.ToString();
        var partitionKey = !string.IsNullOrEmpty(tableId) ? $"Table_{tableId}" : (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 1,
                Window = TimeSpan.FromSeconds(10)
            });
    });

    options.AddPolicy("CustomerOrderPolicy", httpContext =>
    {
        var user = httpContext.User;
        bool isStaff = user.Identity != null && user.Identity.IsAuthenticated && 
                       user.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role && 
                                    (c.Value == "Admin" || c.Value == "Cashier" || c.Value == "Waiter"));
        if (isStaff)
        {
            return RateLimitPartition.GetNoLimiter("NoLimit");
        }

        var tableId = httpContext.Request.RouteValues["tableId"]?.ToString();
        var partitionKey = !string.IsNullOrEmpty(tableId) ? $"Table_{tableId}" : (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                Window = TimeSpan.FromSeconds(30)
            });
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .AddOData(options =>
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

app.UseRateLimiter();

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

// Tự động áp dụng EF Core Migrations khi ứng dụng khởi chạy
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<MenuGoBE.Data.AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Migration Warning] {ex.Message}");
    }
}

// Tự động khởi tạo Lô hàng kế thừa (LEGACY-INIT) cho các bản ghi tồn kho dương hiện có
await MenuGoBE.Data.LegacyBatchSeeder.SeedLegacyBatchesAsync(app.Services);

app.Run();
