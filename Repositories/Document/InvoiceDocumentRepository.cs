using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;

namespace MenuGoBE.Repositories.Document
{
    public class InvoiceDocumentRepository : DocumentRepository, IInvoiceDocumentRepository
    {
        public InvoiceDocumentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
