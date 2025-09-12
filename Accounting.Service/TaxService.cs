using Accounting.Business;
using Accounting.Database;

namespace Accounting.Service
{
  public class TaxService : BaseService
  {
    public TaxService() : base()
    {

    }

    public TaxService(
      string databaseName,
      string databasePassword) : base(databaseName, databasePassword)
    {

    }

    public async Task<Tax> CreateAsync(Tax tax)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetTaxManager().CreateAsync(tax);
    }

    public async Task<(List<Tax> taxes, int? nextPage)> GetAllAsync(int page, int pageSize, int organizationId)
    {
      throw new NotImplementedException();
    }
  }
}