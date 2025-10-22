using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IReserveManager : IGenericRepository<Reserve, int>
  {
    Task<List<Reserve>> GetAllAsync(int organizationId);
  }
}