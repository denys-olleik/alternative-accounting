using Accounting.Common;

namespace Accounting.Business
{
  public class Wallet : IIdentifiable<int>
  {
    public int WalletID { get; set; }
    public string? PublicId { get; set; }
    public string? Password { get; set; }
    public decimal Balance { get; set; }
    public DateTime Created { get; set; }

    public int Identifiable => WalletID;
  }
}