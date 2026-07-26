using System.Threading.Tasks;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Service.Document;

namespace MenuGoBE.Service
{
    public class DocumentService : DocumentServiceBase, IDocumentService
    {
        public DocumentService(IDocumentRepository repo) : base(repo)
        {
        }
    }
}
