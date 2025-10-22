using Accounting.Business;
using Accounting.Database;

namespace Accounting.Service
{
  public class MetalService : BaseService
  {
    public MetalService() : base()
    {

    }

    public MetalService(string databaseName, string databasePassword) 
      : base(databaseName, databasePassword)
    {

    }

    public async Task<Reserve> CreateAsync(Reserve metal)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetMetalManager().CreateAsync(metal);
    }

    public async Task<List<Reserve>> GetAllAsync(int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetMetalManager().GetAllAsync(organizationId);
    }
  }
}