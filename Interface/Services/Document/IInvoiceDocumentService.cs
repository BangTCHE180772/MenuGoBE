using MenuGoBE.Dtos.Document;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Interface.Services.Document
{
    public interface IInvoiceDocumentService
    {
        Task<DocumentEntity> CreateInvoiceAsync(long branchId, List<InvoiceOutflowLineDto> lines, long? createdBy = null);
    }
}
