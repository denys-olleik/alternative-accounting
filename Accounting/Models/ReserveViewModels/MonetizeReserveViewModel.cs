using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.ReserveViewModels
{
  public class MonetizeReserveViewModel
  {
    public int ReserveId { get; set; }
    public decimal Amount { get; set; }
    public List<AccountViewModel> Accounts { get; set; } = new ();

    public ValidationResult ValidationResult { get; set; } = new();

    public class MonetizeReserveViewModelValidator : AbstractValidator<MonetizeReserveViewModel>
    {

    }

    public class AccountViewModel
    {
      public int AccountID { get; set; }
      public string? Name { get; set; }
      public string? Type { get; set; }
    }
  }
}