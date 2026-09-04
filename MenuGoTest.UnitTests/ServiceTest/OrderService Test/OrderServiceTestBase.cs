using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using MenuGoBE.Data;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MenuGoTest.UnitTests.ServiceTest.OrderServiceTest;

/// <summary>
/// Base class providing shared setup for all OrderService tests.
/// Each test file inherits this to get consistent mock configuration.
/// </summary>
public abstract class OrderServiceTestBase
{
    protected readonly Mock<IOrderRepository> _orderRepoMock;
    protected readonly Mock<ITableRepository> _tableRepoMock;
    protected readonly Mock<ICustomerRepository> _customerRepoMock;
    protected readonly Mock<IPaymentRepository> _paymentRepoMock;
    protected readonly Mock<IVoucherRepository> _voucherRepoMock;
    protected readonly Mock<IReservationRepository> _reservationRepoMock;
    protected readonly Mock<IProductRepository> _productRepoMock;
    protected readonly Mock<IAreaRepository> _areaRepoMock;
    protected readonly Mock<IBInventoryRepository> _bInventoryRepoMock;
    protected readonly Mock<IDocumentRepository> _documentRepoMock;
    protected readonly Mock<MenuGoBE.Interface.Services.Document.IDocumentService> _documentServiceMock;
    protected readonly Mock<IPartnerRepository> _partnerRepoMock;
    protected readonly IMapper _mapper;
    protected readonly OrderService _service;

    protected OrderServiceTestBase()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _tableRepoMock = new Mock<ITableRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _voucherRepoMock = new Mock<IVoucherRepository>();
        _reservationRepoMock = new Mock<IReservationRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _areaRepoMock = new Mock<IAreaRepository>();
        _bInventoryRepoMock = new Mock<IBInventoryRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _documentServiceMock = new Mock<MenuGoBE.Interface.Services.Document.IDocumentService>();
        _partnerRepoMock = new Mock<IPartnerRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);

        _service = new OrderService(
            _orderRepoMock.Object, _mapper, _tableRepoMock.Object,
            _customerRepoMock.Object, _paymentRepoMock.Object,
            _voucherRepoMock.Object, _reservationRepoMock.Object,
            _productRepoMock.Object, _areaRepoMock.Object,
            _bInventoryRepoMock.Object,
            _documentRepoMock.Object, _partnerRepoMock.Object,
            dbContext, _documentServiceMock.Object,
            Options.Create(new PointSystemConfig()));

        var transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        _orderRepoMock.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
        _orderRepoMock.Setup(r => r.CreateExecutionStrategy()).Returns(dbContext.Database.CreateExecutionStrategy());
    }
}
