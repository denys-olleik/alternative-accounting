using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.MetalViewModels
{
  public class DepositMetalViewModel
  {
    public string Name { get; set; }
    public string Type { get; set; }
    public decimal Weight { get; set; }
    public string Unit { get; set; } = string.Empty;
    public List<string> AvailableUnits { get; set; } = new ();

    public ValidationResult ValidationResult = new();

    public class DepositMetalViewModelValidator : AbstractValidator<DepositMetalViewModel>
    {
      // required: type, weight, and unit
    }
  }
}