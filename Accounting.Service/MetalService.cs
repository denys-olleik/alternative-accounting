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

    public async Task<Metal> CreateAsync(Metal metal)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetMetalManager().CreateAsync(metal);
    }
  }
}