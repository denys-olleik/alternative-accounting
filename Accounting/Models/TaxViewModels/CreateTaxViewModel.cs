using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.TaxViewModels
{
  public class CreateTaxViewModel
  {
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Rate { get; set; }
    public List<Account>? Accounts { get; set; }
    public int SelectedAccountId { get; set; }
    public List<Item>? Items { get; set; }
    public int? SelectedItemId { get; set; }
    public List<Location>? Locations { get; set; }
    public int? SelectedLocationId { get; set; }
    public ValidationResult? ValidationResult { get; set; }

    public class Item
    {
      public int ItemID { get; set; }
      public string Name { get; set; } = null!;
    }

    public class Location
    {
      public int LocationID { get; set; }
      public string Name { get; set; } = null!;
    }

    public class Account
    {
      public int AccountID { get; set; }
      public string Name { get; set; } = null!;
      public string Type { get; set; } = null!;
    }

    // required: name, rate, inventoryId, and accountId
    public class CreateTaxViewModelValidator : AbstractValidator<CreateTaxViewModel>
    {
      public CreateTaxViewModelValidator()
      {
        RuleFor(x => x.Name)
          .NotEmpty()
          .WithMessage("Name is required.");
        RuleFor(x => x.Rate)
          .NotEmpty()
          .WithMessage("Rate is required.");
        RuleFor(x => x.SelectedItemId)
          .NotEmpty()
          .WithMessage("Item is required.");
        RuleFor(x => x.SelectedAccountId)
          .NotEmpty()
          .WithMessage("Account is required.");
        RuleFor(x => x.SelectedLocationId)
          .NotEmpty()
          .WithMessage("Location is required.");
      }
    }
  }
}