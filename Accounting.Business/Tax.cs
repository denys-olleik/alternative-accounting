using Accounting.Common;

namespace Accounting.Business
{
  public class Tax : IIdentifiable<int>
  {
    public int TaxID { get; set; }
    public int ItemId { get; set; }
    public int? LocationId { get; set; }
    public int LiabilityAccountId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Rate { get; set; }

    public DateTime Created { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }
    public int Identifiable => this.TaxID;
  }
}