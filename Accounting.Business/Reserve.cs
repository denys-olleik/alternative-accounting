using Accounting.Common;

namespace Accounting.Business
{
  public class Reserve : IIdentifiable<int>
  {
    public int ReserveID { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public decimal Weight { get; set; }
    public string Unit { get; set; }
    public DateTime Created { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }

    public int Identifiable => this.ReserveID;
  }
}