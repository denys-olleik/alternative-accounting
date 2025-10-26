using FluentValidation.Results;

namespace Accounting.Models.ReserveViewModels
{
  public class MonetizeReserveViewModel
  {


    public ValidationResult ValidationResult { get; set; } = new();
  }
}