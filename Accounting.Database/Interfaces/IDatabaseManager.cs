using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IDatabaseManager : IGenericRepository<DatabaseThing, string>
  {
    Task<string> BackupDatabaseAsync(string databaseName);
    Task<DatabaseThing> CreateDatabase(string tenantId);
    System.Threading.Tasks.Task DeleteAsync(string databaseName);
    System.Threading.Tasks.Task ResetDatabaseAsync();
    System.Threading.Tasks.Task RestoreDatabase(string databaseName, Common.File file);
    System.Threading.Tasks.Task RunSQLScript(string script, string databaseName);
  }
}