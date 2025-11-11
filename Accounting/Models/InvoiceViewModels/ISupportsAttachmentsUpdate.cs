namespace Accounting.Models.InvoiceViewModels
{
  public interface ISupportsAttachmentsUpdate
  {
    public string? DeletedAttachmentIdsCsv { get; set; }
    public string? NewAttachmentIdsCsv { get; set; }
    public List<InvoiceAttachmentViewModel> Attachments { get; set; }

    public class InvoiceAttachmentViewModel
    {
      public int InvoiceAttachmentID { get; set; }
      public string FileName { get; set; }
    }
  }
}