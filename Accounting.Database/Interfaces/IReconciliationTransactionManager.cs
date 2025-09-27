using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IReconciliationTransactionManager : IGenericRepository<ReconciliationTransaction, int>
  {
    Task<List<ReconciliationTransaction>> GetAllByIdAsync(int reconciliationId, int organizationId);
    Task<ReconciliationTransaction> GetAsync(int reconciliationTransactionId, int organizationId);
    Task<(List<ReconciliationTransaction> reconciliationTransactions, int? nextPage)> GetReconciliationTransactionAsync(int reconciliationId, int page, int pageSize, int organizationId);
    Task<int> ImportAsync(List<ReconciliationTransaction> reconciliationTransactions);
    Task<int> ImportAsync(ReconciliationTransaction transaction);
    Task<int> UpdateReconciliationTransactionInstructionAsync(int reconciliationTransactionID, string reconciliationInstructionJSON);
  }
}