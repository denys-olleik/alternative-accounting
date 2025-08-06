using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IPlayerManager : IGenericRepository<Player, int>
  {
    Task<string?> GetCountryAsync(string ipAddress, int withinLastDays);
    Task<List<Player>> GetPlayersAsync(int withinLastSeconds);
    Task<List<Player>> GetSectorClaims();
    System.Threading.Tasks.Task ReportPosition(string userId, int x, int y, string ipAddress, string country, bool claim);
    Task<Player?> GetCurrentOccupantAsync(int sectorX, int sectorY);
  }
}