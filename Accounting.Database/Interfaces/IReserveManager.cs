using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IReserveManager : IGenericRepository<Reserve, int>
  {
    Task<List<Reserve>> GetAllAsync(int organizationId);
    Task<Reserve> GetAsync(int reserveId, int organizationId);
  }
}