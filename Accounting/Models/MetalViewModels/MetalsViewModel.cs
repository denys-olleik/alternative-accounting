namespace Accounting.Models.MetalViewModels
{
  public class MetalsViewModel
  {
    public List<ReserveViewModel> Metals { get; set; } = new();

    public class ReserveViewModel
    {
      public int ReserveID { get; set; }
      public string Name { get; set; }
      public string Type { get; set; }
      public decimal Weight { get; set; }
      public string Unit { get; set; }
    }
  }
}