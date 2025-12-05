namespace Accounting.Models
{
  public interface ISupportsAttachmentsUpdate
  {
    public string? DeletedAttachmentIdsCsv { get; set; }
    public string? NewAttachmentIdsCsv { get; set; }
    public List<IAttachmentViewModel> Attachments { get; set; }
  }
}