using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IJournalInvoiceInvoiceLineManager : IGenericRepository<JournalInvoiceInvoiceLine, int>
  { 
    Task<List<InvoiceLine>> GetByInvoiceIdAsync(int invoiceId, int organizationId, bool onlyCurrent);
    Task<List<JournalInvoiceInvoiceLine>> GetByInvoiceIdAsync(int invoiceId, int organizationId);
    Task<List<JournalInvoiceInvoiceLine>> GetLastTransactionAsync(int invoiceLineID, int organizationId, bool loadChildren = false);
  }
}