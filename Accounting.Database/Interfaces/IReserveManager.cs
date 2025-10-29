using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IReserveManager : IGenericRepository<Reserve, int>
  {
    Task<decimal> GetUnmonetizedWeightAsync(string type, int organizationId);
    Task<List<Reserve>> GetAllAsync(int organizationId);
    Task<Reserve> GetAsync(int reserveId, int organizationId);
  }
}