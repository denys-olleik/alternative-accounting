using Accounting.Business;

namespace Accounting.Models.BlogViewModels
{
  public class BlogAttachmentViewModel : IAttachmentViewModel
  {
    public int BlogAttachmentID { get; set; }
    public string FileName { get; set; }

    // Store the whole per-variant status map (mp3/720p/original) as JSON.
    // This keeps the UI simple and lets the server remain authoritative.
    public string? TranscodeStatusJSONB { get; set; }

    // Optional: convenience typed view for Vue (so you don’t parse JSON everywhere).
    // If you don’t want to send both, you can omit this and only send TranscodeStatusJSONB.
    public Dictionary<string, TranscodeStatus>? TranscodeStatusByVariant { get; set; }
  }
}