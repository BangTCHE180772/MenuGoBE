using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;

namespace MenuGoBE.Repositories.Document
{
    public class ExportDeleteDocumentRepository : DocumentRepository, IExportDeleteDocumentRepository
    {
        public ExportDeleteDocumentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
