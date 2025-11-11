

```cs
namespace Accounting.Models.InvoiceViewModels
{
  public interface ISupportsAttachments
  {
  }
}
```

```cs
using System.Collections.Generic;

namespace Accounting.Models.InvoiceViewModels
{
  public partial class UpdateInvoiceViewModel
  {
    public string? DeletedAttachmentIdsCsv { get; set; }
    public string? NewAttachmentIdsCsv { get; set; }

    public List<InvoiceAttachmentViewModel> Attachments { get; set; } = new ();

    public class InvoiceAttachmentViewModel
    {
      public int InvoiceAttachmentID { get; set; }
      public string FileName { get; set; }
    }
  }
}
```

