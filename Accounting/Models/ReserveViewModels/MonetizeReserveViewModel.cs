using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.ReserveViewModels
{
  public class MonetizeReserveViewModel
  {
    public int ReserveId { get; set; }
    public decimal Amount { get; set; }

    public ValidationResult ValidationResult { get; set; } = new();

    public class MonetizeReserveViewModelValidator : AbstractValidator<MonetizeReserveViewModel>
    {

    }
  }
}