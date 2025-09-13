using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface ITaxManager : IGenericRepository<Tax, int>
  {
    Task<(List<Tax> taxes, int? nextPage)> GetAllAsync(int page, int pageSize, int organizationId);
  }
}