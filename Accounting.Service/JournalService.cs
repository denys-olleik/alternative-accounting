using Accounting.Business;
using Accounting.Database;

namespace Accounting.Service
{
  public class JournalService : BaseService
  {
    public JournalService()
    {
      
    }

    public JournalService(
      string databaseName, 
      string databasePassword) : base(databaseName, databasePassword)
    {
      
    }

    public async Task<Journal> CreateAsync(Journal journal)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetJournalManager().CreateAsync(journal);
    }

    public async Task<bool> HasEntriesUpToRootAsync(int accountId, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetJournalManager().HasEntriesAsync(accountId, organizationId);
    }

    public async Task<(List<JournalTransaction> journalTransactions, int? nextPage)> GetAllUnionAsync(int page, int pageSize, int getOrganizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetJournalManager().GetAllUnionAsync(page, pageSize, getOrganizationId);
    }

    public async Task<List<Journal>> GetByTransactionGuid(string transactionGuid, int organizationId)
    {
      var factoryManger = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManger.GetJournalManager().GetByTransactionGuid(transactionGuid, organizationId);
    }
  }
}