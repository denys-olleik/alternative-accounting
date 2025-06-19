using Accounting.Business;
using Accounting.Database;

namespace Accounting.Service
{
  public class WalletService : BaseService
  {
    public WalletService() : base()
    {

    }
    public WalletService(string databaseName, string databasePassword) : base(databaseName, databasePassword)
    {

    }

    public async Task<Wallet> TransferAsync(string? publicId, string? destinationWalletPublicId, decimal amount, string? password)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetWalletManager()
          .TransferAsync(publicId, destinationWalletPublicId, amount, password);
    }
  }
}