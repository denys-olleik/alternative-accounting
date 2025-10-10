using Accounting.Common;

namespace Accounting.Business
{
  public class Metal : IIdentifiable<int>
  {
    public int MetalID { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public decimal Weight { get; set; }
    public string Unit { get; set; }
    public DateTime Created { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }

    public int Identifiable => this.MetalID;
  }
}