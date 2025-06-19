using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IWalletManager : IGenericRepository<Wallet, int>
  {
    Task<Wallet> TransferAsync(string? publicId, string? destinationWalletPublicId, decimal amount, string? password);
  }
}