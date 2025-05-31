namespace Accounting.Service
{
  public class PlayerService : BaseService
  {
    public PlayerService() : base()
    {
    }

    public PlayerService(string databaseName, string databasePassword)
      : base(databaseName, databasePassword)
    {

    }
  }
}