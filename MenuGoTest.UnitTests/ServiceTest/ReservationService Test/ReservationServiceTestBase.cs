using AutoMapper;
using MenuGoBE.Hubs;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Mapper;
using MenuGoBE.Service;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MenuGoTest.UnitTests.ServiceTest.ReservationServiceTest;

public abstract class ReservationServiceTestBase
{
    protected readonly Mock<IReservationRepository> _reservationRepoMock;
    protected readonly Mock<IOrderRepository> _orderRepoMock;
    protected readonly Mock<ITableRepository> _tableRepoMock;
    protected readonly Mock<IOrderDetailRepository> _detailRepoMock;
    protected readonly Mock<IProductRepository> _productRepoMock;
    protected readonly Mock<ICustomerRepository> _customerRepoMock;
    protected readonly Mock<IReservationSignalService> _signalServiceMock;
    protected readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
    protected readonly Mock<INotificationRepository> _notificationRepoMock;
    protected readonly Mock<IOrderDetailService> _orderDetailServiceMock;
    protected readonly IMapper _mapper;
    protected readonly ReservationService _service;

    protected ReservationServiceTestBase()
    {
        _reservationRepoMock = new Mock<IReservationRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _tableRepoMock = new Mock<ITableRepository>();
        _detailRepoMock = new Mock<IOrderDetailRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _signalServiceMock = new Mock<IReservationSignalService>();
        _hubContextMock = new Mock<IHubContext<NotificationHub>>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _orderDetailServiceMock = new Mock<IOrderDetailService>();

        // Setup HubContext mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);

        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<AutoMapperProfiles>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _service = new ReservationService(
            _reservationRepoMock.Object, _orderRepoMock.Object, _tableRepoMock.Object,
            _detailRepoMock.Object, _productRepoMock.Object, _customerRepoMock.Object,
            _mapper, _signalServiceMock.Object, _hubContextMock.Object, _notificationRepoMock.Object,
            _orderDetailServiceMock.Object);
    }
}
