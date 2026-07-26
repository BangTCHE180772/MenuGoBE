using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;

namespace MenuGoBE.Repositories.Document
{
    public class ProductionDocumentRepository : DocumentRepository, IProductionDocumentRepository
    {
        public ProductionDocumentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
