using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IReconciliationManager : IGenericRepository<Reconciliation, int>
  {
    Task<(List<Reconciliation> reconciliations, int? nextPage)> GetAllAsync(int page, int pageSize, int organizationId);
    Task<List<Reconciliation>> GetAllDescendingAsync(int top, int organizationId);
    Task<Reconciliation> GetByIdAsync(int id, int organizationId);
  }
}