using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IJournalReconciliationTransactionManager : IGenericRepository<JournalReconciliationTransactionDebitCredit, int>
  {
    Task<List<JournalReconciliationTransactionDebitCredit>> GetLastTransactionAsync(int reconciliationTransactionId, int organizationId, bool loadChildren = false);
  }
}