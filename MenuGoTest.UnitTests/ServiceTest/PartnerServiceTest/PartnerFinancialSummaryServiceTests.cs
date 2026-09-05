using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.PartnerServiceTest
{
    public class PartnerFinancialSummaryServiceTests
    {
        private readonly Mock<IPartnerRepository> _partnerRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly AppDbContext _context;
        private readonly PartnerService _service;

        public PartnerFinancialSummaryServiceTests()
        {
            _partnerRepoMock = new Mock<IPartnerRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _service = new PartnerService(_partnerRepoMock.Object, _branchRepoMock.Object, _context);
        }

        #region Kịch bản 1: Cửa hàng nợ NCC (Dư nợ > 0)
        [Fact]
        public async Task GetPartnerFinancialSummaryAsync_StoreOwesSupplier_ReturnsPositiveRemainingDebt()
        {
            // Nhập 1.000.000, đã trả 400.000, không trả hàng
            long partnerId = 1;
            _partnerRepoMock.Setup(r => r.GetPartnerByIdAsync(partnerId))
                .ReturnsAsync(new Partner { Id = partnerId, Name = "NCC A", Type = PartnerType.Supplier });

            _context.Documents.Add(new Document
            {
                Id = 1,
                PartnerId = partnerId,
                Type = DocumentType.Import,
                Status = DocumentStatus.Completed,
                TotalAmount = 1000000m,
                AmountPaid = 400000m
            });
            await _context.SaveChangesAsync();

            var summary = await _service.GetPartnerFinancialSummaryAsync(partnerId);

            Assert.Equal(1000000m, summary.TotalImportAmount);
            Assert.Equal(0m, summary.TotalReturnAmount);
            Assert.Equal(400000m, summary.TotalPaidAmount);
            Assert.Equal(0m, summary.TotalReceivedAmount);
            Assert.Equal(600000m, summary.RemainingDebt);
        }
        #endregion

        #region Kịch bản 2: NCC nợ lại cửa hàng (Dư nợ < 0)
        [Fact]
        public async Task GetPartnerFinancialSummaryAsync_SupplierOwesStore_ReturnsNegativeRemainingDebt()
        {
            // Nhập 1.000.000, đã trả 1.000.000. Sau đó trả hàng 300.000, NCC chưa hoàn tiền (AmountPaid = 0)
            // Dư Nợ = (1.000.000 - 300.000) - (1.000.000 - 0) = 700.000 - 1.000.000 = -300.000
            long partnerId = 2;
            _partnerRepoMock.Setup(r => r.GetPartnerByIdAsync(partnerId))
                .ReturnsAsync(new Partner { Id = partnerId, Name = "NCC B", Type = PartnerType.Supplier });

            _context.Documents.Add(new Document
            {
                Id = 2,
                PartnerId = partnerId,
                Type = DocumentType.Import,
                Status = DocumentStatus.Completed,
                TotalAmount = 1000000m,
                AmountPaid = 1000000m
            });
            _context.Documents.Add(new Document
            {
                Id = 3,
                PartnerId = partnerId,
                Type = DocumentType.Return,
                Status = DocumentStatus.Completed,
                TotalAmount = 300000m,
                AmountPaid = 0m
            });
            await _context.SaveChangesAsync();

            var summary = await _service.GetPartnerFinancialSummaryAsync(partnerId);

            Assert.Equal(1000000m, summary.TotalImportAmount);
            Assert.Equal(300000m, summary.TotalReturnAmount);
            Assert.Equal(1000000m, summary.TotalPaidAmount);
            Assert.Equal(0m, summary.TotalReceivedAmount);
            Assert.Equal(-300000m, summary.RemainingDebt);
        }
        #endregion

        #region Kịch bản 3: Hết nợ (Dư nợ == 0)
        [Fact]
        public async Task GetPartnerFinancialSummaryAsync_AllSettled_ReturnsZeroRemainingDebt()
        {
            // Nhập 1.000.000, đã trả 1.000.000. Trả hàng 300.000, NCC đã hoàn lại tiền mặt 300.000
            // Dư Nợ = (1.000.000 - 300.000) - (1.000.000 - 300.000) = 700.000 - 700.000 = 0
            long partnerId = 3;
            _partnerRepoMock.Setup(r => r.GetPartnerByIdAsync(partnerId))
                .ReturnsAsync(new Partner { Id = partnerId, Name = "NCC C", Type = PartnerType.Supplier });

            _context.Documents.Add(new Document
            {
                Id = 4,
                PartnerId = partnerId,
                Type = DocumentType.Import,
                Status = DocumentStatus.Completed,
                TotalAmount = 1000000m,
                AmountPaid = 1000000m
            });
            _context.Documents.Add(new Document
            {
                Id = 5,
                PartnerId = partnerId,
                Type = DocumentType.Return,
                Status = DocumentStatus.Completed,
                TotalAmount = 300000m,
                AmountPaid = 300000m
            });
            await _context.SaveChangesAsync();

            var summary = await _service.GetPartnerFinancialSummaryAsync(partnerId);

            Assert.Equal(1000000m, summary.TotalImportAmount);
            Assert.Equal(300000m, summary.TotalReturnAmount);
            Assert.Equal(1000000m, summary.TotalPaidAmount);
            Assert.Equal(300000m, summary.TotalReceivedAmount);
            Assert.Equal(0m, summary.RemainingDebt);
        }
        #endregion
    }
}
