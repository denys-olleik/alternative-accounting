using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IPlayerManager : IGenericRepository<Player, int>
  {
    Task<List<Player>> GetPlayersAsync(int withinLastSeconds);
    Task ReportPosition(int x, int y);
  }
}