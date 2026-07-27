using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;

namespace MenuGoBE.Repositories.Document
{
    public class CheckDocumentRepository : DocumentRepository, ICheckDocumentRepository
    {
        public CheckDocumentRepository(AppDbContext context) : base(context)
        {
        }
    }
}
