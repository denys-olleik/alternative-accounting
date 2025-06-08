using Accounting.Common;

namespace Accounting.Business
{
  public class Player : IIdentifiable<int>
  {
    public int PlayerID { get; set; }
    public string UserId { get; set; } = null!;
    public DateTime? OccupyUntil { get; set; }
    public string? IpAddress { get; set; }
    public string? Country { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Identifiable => PlayerID;
  }
}