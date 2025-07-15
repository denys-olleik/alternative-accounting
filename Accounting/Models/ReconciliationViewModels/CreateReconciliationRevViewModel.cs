using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.ReconciliationViewModels
{
  public class CreateReconciliationRevViewModel 
  {
    public int? ReconciliationID { get; set; }

    public IFormFile? StatementCsv { get; set; }

    public ValidationResult? ValidationResult { get; set; }

    public class CreateReconciliationViewModelValidator : AbstractValidator<CreateReconciliationRevViewModel>
    {
      public CreateReconciliationViewModelValidator()
      {
        RuleFor(x => x.StatementCsv)
          .Must(file => file != null && file.Length > 0).WithMessage("CSV file cannot be empty.");
      }
    }
  }
}