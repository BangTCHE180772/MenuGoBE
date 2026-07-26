using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories.Document
{
    public class TransferDocumentRepository : DocumentRepository, ITransferDocumentRepository
    {
        public TransferDocumentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Document?> GetChildReceiveDocumentAsync(long parentTransferDocumentId)
        {
            return await _context.Documents
                .Include(d => d.Branch)
                .Include(d => d.DocumentDetails)
                .FirstOrDefaultAsync(d => d.ParentDocumentId == parentTransferDocumentId && d.Type == DocumentType.Transfer && !d.IsDeleted);
        }
    }
}
