using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.ReconciliationViewModels
{
  public class CreateReconciliationRevViewModel
  {
    public int? ReconciliationID { get; set; }

    public string Name { get; set; }

    public IFormFile? StatementCsv { get; set; }

    public string? StatementCsvText { get; set; }

    public ValidationResult? ValidationResult { get; set; }

    public class CreateReconciliationViewModelValidator : AbstractValidator<CreateReconciliationRevViewModel>
    {
      public CreateReconciliationViewModelValidator()
      {
        RuleFor(x => x.Name)
          .NotEmpty().WithMessage("'Name' is required.")
          .MaximumLength(200).WithMessage("'Name' must be within 200 characters.");

        RuleFor(x => new { x.StatementCsv, x.StatementCsvText })
          .Must(x =>
            (x.StatementCsv != null && x.StatementCsv.Length > 0 && string.IsNullOrWhiteSpace(x.StatementCsvText)) ||
            (x.StatementCsv == null && !string.IsNullOrWhiteSpace(x.StatementCsvText))
          )
          .WithMessage("Upload file or content.");
      }
    }
  }
}