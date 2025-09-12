namespace Accounting.Models.TaxViewModels
{
  public class TaxViewModel
  {
    public int TaxID { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }  
    public decimal Rate { get; set; }
    public int ItemId { get; set; }
    public ItemViewModel Item { get; set; } = null!;
    public int LiabilityAccountId { get; set; }
    public AccountViewModel LiabilityAccount { get; set; } = null!;
    public int? LocationId { get; set; }
    public LocationViewModel Location { get; set; } = null!;

    public class ItemViewModel
    {
      public int ItemID { get; set; }
      public string Name { get; set; } = null!;
    }

    public class AccountViewModel
    {
      public int AccountID { get; set; }
      public string Name { get; set; } = null!;
      public string Type { get; set; } = null!;
    }

    public class LocationViewModel
    {
      public int LocationID { get; set; }
      public string Name { get; set; } = null!;
    }
  }
}