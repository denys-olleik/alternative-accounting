
using Accounting.Business;
using Accounting.Database;
using Accounting.Database.Interfaces;

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

    public async Task<string?> GetCountryAsync(string ipAddress, int withinLastDays)
    {
      FactoryManager factoryManager = new FactoryManager(_databaseName, _databasePassword);
      string? country = await factoryManager.GetPlayerManager().GetCountryAsync(ipAddress, withinLastDays);
      return country;
    }

    public async Task<List<Player>> GetPlayersAsync(int withinLastSeconds)
    {
      FactoryManager factoryManager = new FactoryManager(_databaseName, _databasePassword);
      IPlayerManager playerManager = factoryManager.GetPlayerManager();
      List<Player> players = await playerManager.GetPlayersAsync(withinLastSeconds);
      return players;
    }

    public async Task<List<Player>> GetVotesAsync(int withinLastSeconds)
    {
      FactoryManager factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetPlayerManager().GetVotesAsync(withinLastSeconds);
    }

    public async Task ReportPosition(string userId, int x, int y, string ipAddress, string country, bool vote)
    {
      FactoryManager factoryManager = new FactoryManager(_databaseName, _databasePassword);
      await factoryManager.GetPlayerManager().ReportPosition(userId, x, y, ipAddress, country, vote);
    }
  }
}