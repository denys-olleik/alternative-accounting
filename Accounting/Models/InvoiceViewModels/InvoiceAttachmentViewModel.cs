namespace Accounting.Models.InvoiceViewModels
{
  public class InvoiceAttachmentViewModel : IAttachmentViewModel
  {
    public int InvoiceAttachmentID { get; set; }
    public string FileName { get; set; }
  }
}