using Accounting.Business;
using Accounting.Database;

namespace Accounting.Service
{
  public class ReserveService : BaseService
  {
    public ReserveService() : base()
    {

    }

    public ReserveService(string databaseName, string databasePassword) 
      : base(databaseName, databasePassword)
    {

    }

    public async Task<Reserve> CreateAsync(Reserve metal)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetReserveManager().CreateAsync(metal);
    }

    public async Task<List<Reserve>> GetAllAsync(int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetReserveManager().GetAllAsync(organizationId);
    }

    public async Task<Reserve> GetAsync(int reserveId, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetReserveManager().GetAsync(reserveId, organizationId);
    }
  }
}