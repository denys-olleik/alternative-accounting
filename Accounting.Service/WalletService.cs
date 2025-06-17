using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
  }
}