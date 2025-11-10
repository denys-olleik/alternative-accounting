
```cs
using Accounting.Models.AddressViewModels;
using Accounting.Models.BusinessEntityViewModels;
using Accounting.Models.Item;
using Accounting.Models.PaymentTermViewModels;
using FluentValidation.Results;

namespace Accounting.Models.InvoiceViewModels
{
  public class UpdateInvoiceViewModel
  {
    public int ID { get; set; }
    public BusinessEntityViewModel? Customer { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? SelectedCustomerId { get; set; }
    public int? SelectedAddressId { get; set; }
    public AddressViewModel? BillingAddress { get; set; }
    public AddressViewModel? ShippingAddress { get; set; }
    public List<string>? InvoiceStatuses { get; set; }
    public string? InvoiceLinesJson { get; set; }
    public string? DeletedInvoiceLinesJson { get; set; }
    public List<ItemViewModel>? ProductsAndServices { get; set; }
    public List<InvoiceLineViewModel>? ExistingInvoiceLines { get; set; }
    public List<InvoiceLineViewModel>? NewInvoiceLines { get; set; }
    public List<InvoiceLineViewModel>? DeletedInvoiceLines { get; set; }
    public List<PaymentTermViewModel>? PaymentTerms { get; set; }
    public PaymentTermViewModel? SelectedPaymentTerm { get; set; }
    public string? SelectedPaymentTermJSON { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime LastUpdated { get; set; }

    public string? DeletedAttachmentIdsCsv { get; set; }
    public string? NewAttachmentIdsCsv { get; set; }

    public List<InvoiceAttachmentViewModel> Attachments { get; set; } = new List<InvoiceAttachmentViewModel>();

    public ValidationResult? ValidationResult { get; set; }

    public class InvoiceAttachmentViewModel
    {
      public int InvoiceAttachmentID { get; set; }
      public string FileName { get; set; }
    }
  }
}
```

does this make sensen to create a partial class for attachment related props? respond with one sentence.
---
Yes, creating a partial class for attachment-related properties makes sense as it helps separate concerns and keeps the codebase organized, especially if attachment logic grows more complex.
`gpt-4.1-2025-04-14`,`{tokens: 506/474/32}`
---
`UpdateInvoiceViewModel.cs` how should the file containgng partial class be named? that's the original file.
---
The file containing the partial class for attachment-related properties should be named UpdateInvoiceViewModel.Attachments.cs to clearly indicate its association with UpdateInvoiceViewModel and its focus on attachments.
`gpt-4.1-2025-04-14`,`{tokens: 596/560/36}`
---
give me the partial class so i can copy paste and test it.
---
Certainly! Here’s the partial class for the attachment-related properties, ready to copy and paste:

```cs
using System.Collections.Generic;

namespace Accounting.Models.InvoiceViewModels
{
    public partial class UpdateInvoiceViewModel
    {
        public string? DeletedAttachmentIdsCsv { get; set; }
        public string? NewAttachmentIdsCsv { get; set; }

        public List<InvoiceAttachmentViewModel> Attachments { get; set; } = new List<InvoiceAttachmentViewModel>();

        public class InvoiceAttachmentViewModel
        {
            public int InvoiceAttachmentID { get; set; }
            public string FileName { get; set; }
        }
    }
}
```

Place this in a file named UpdateInvoiceViewModel.Attachments.cs.
`gpt-4.1-2025-04-14`,`{tokens: 794/640/154}`
---
