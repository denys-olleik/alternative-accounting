using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IJournalReconciliationTransactionManager : IGenericRepository<JournalReconciliationTransaction, int>
  {
    Task<JournalReconciliationTransaction> CreateAsync(JournalReconciliationTransaction journalExpense, bool loadJournal);
    Task<List<JournalReconciliationTransaction>> GetLastTransactionAsync(int reconciliationTransactionId, int organizationId, bool loadChildren = false);
  }
}