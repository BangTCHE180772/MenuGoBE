using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Service;
using Moq;

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
    protected readonly Mock<MenuGoBE.Interface.Services.IRedisInventoryService> _redisInventoryServiceMock;
    protected readonly Mock<IBInventoryRepository> _bInventoryRepoMock;
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
        _redisInventoryServiceMock = new Mock<MenuGoBE.Interface.Services.IRedisInventoryService>();
        _bInventoryRepoMock = new Mock<IBInventoryRepository>();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _service = new OrderService(
            _orderRepoMock.Object, _mapper, _tableRepoMock.Object,
            _customerRepoMock.Object, _paymentRepoMock.Object,
            _voucherRepoMock.Object, _reservationRepoMock.Object,
            _productRepoMock.Object, _areaRepoMock.Object,
            _redisInventoryServiceMock.Object, _bInventoryRepoMock.Object);
    }
}
