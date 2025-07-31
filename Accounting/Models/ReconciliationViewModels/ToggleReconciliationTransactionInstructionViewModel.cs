using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.ReconciliationViewModels
{
  public class ToggleReconciliationTransactionInstructionViewModel
  {
    public int ReconciliationTransactionID { get; set; }
    public string DebitAccount { get; set; }
    public string CreditAccount { get; set; }

    public ValidationResult ValidationResult { get; set; } = new();

    public class ToggleReconciliationTransactionInstructionViewModelValidator 
      : AbstractValidator<ToggleReconciliationTransactionInstructionViewModel>
    {
      public ToggleReconciliationTransactionInstructionViewModelValidator()
      {
        RuleFor(x => x.ReconciliationTransactionID).GreaterThan(0);
        RuleFor(x => x.DebitAccount).MinimumLength(1);
        RuleFor(x => x.CreditAccount).MinimumLength(1);
      }
    }
  }
}