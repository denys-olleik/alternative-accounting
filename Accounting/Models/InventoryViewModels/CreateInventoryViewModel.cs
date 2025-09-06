namespace Accounting.Models.InventoryViewModels
{
  public class CreateInventoryViewModel
  {
    public int ItemId { get; set; }
    public List<LocationViewModel> Locations { get; set; } = new();
    public int? SelectedLocationId { get; set; }
    public int Quantity { get; set; }
    public int PriceAssetIndividual { get; set; }
    public int PriceAssetTotal { get; set; }
    public decimal? PriceInvoice { get; set; }

    public class LocationViewModel
    {
      public int LocationID { get; set; }
      public string Name { get; set; } = null!;
    }
  }
}