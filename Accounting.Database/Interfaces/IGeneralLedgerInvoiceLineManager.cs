using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IJournalInvoiceInvoiceLineManager : IGenericRepository<JournalInvoiceInvoiceLine, int>
  {
    Task<JournalInvoiceInvoiceLine> GetAsync(int journalInvoiceInvoiceLineId, int orgId);
    Task<List<InvoiceLine>> GetByInvoiceIdAsync(int invoiceId, int organizationId, bool onlyCurrent);
    Task<List<JournalInvoiceInvoiceLine>> GetByInvoiceIdAsync(int invoiceId, int organizationId);
    Task<List<Journal>> GetByTransactionGuid(Guid transactionGuid, int orgId);
    Task<List<JournalInvoiceInvoiceLine>> GetLastTransactionAsync(int invoiceLineID, int organizationId, bool loadChildren = false);
  }
}