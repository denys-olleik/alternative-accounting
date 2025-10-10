namespace Accounting.Models.MetalViewModels
{
  public class MetalsViewModel
  {
    public List<MetalViewModel> Metals { get; set; } = new();

    public class MetalViewModel
    {
      public int MetalID { get; set; }
      public string Name { get; set; }
      public string Type { get; set; }
      public decimal Weight { get; set; }
      public string Unit { get; set; }
    }
  }
}