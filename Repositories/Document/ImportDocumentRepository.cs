using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories.Document
{
    public class ImportDocumentRepository : DocumentRepository, IImportDocumentRepository
    {
        public ImportDocumentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> HasReturnDocumentsAsync(long importDocumentId)
        {
            return await _context.Documents.AnyAsync(d => d.ParentDocumentId == importDocumentId && !d.IsDeleted);
        }
    }
}
