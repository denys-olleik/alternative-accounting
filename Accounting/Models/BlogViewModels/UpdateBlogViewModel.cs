using Accounting.Models.InvoiceViewModels;
using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.BlogViewModels
{
  public class UpdateBlogViewModel : ISupportsAttachments
  {
    public int BlogID { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public bool Public { get; set; }

    public ValidationResult ValidationResult { get; set; } = new();
    #region ISupportsAttachments
    public string? DeletedAttachmentIdsCsv { get; set; }
    public string? NewAttachmentIdsCsv { get; set; }
    public List<ISupportsAttachments.InvoiceAttachmentViewModel> Attachments { get; set; } = new();
    #endregion

    public class UpdateBlogViewModelValidator : AbstractValidator<UpdateBlogViewModel>
    {
      public UpdateBlogViewModelValidator()
      {
        RuleFor(x => x.Title)
          .NotEmpty().WithMessage("Title is required.");

        RuleFor(x => x.Content)
          .NotEmpty().WithMessage("Content is required.");
      }
    }
  }
}