using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IPlayerManager : IGenericRepository<Player, int>
  {
    Task<string?> GetCountryAsync(string ipAddress, int withinLastDays);
    Task<List<Player>> GetPlayersAsync(int withinLastSeconds);
    Task ReportPosition(string userId, int x, int y, string ipAddress, string country);
  }
}