using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.TaxViewModels;
using Accounting.Service;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("tax")]
  public class TaxController : BaseController
  {
    private readonly AccountService _accountService;

    public TaxController(RequestContext requestContext)
    {
      _accountService = new AccountService(requestContext.DatabaseName!, requestContext!.DatabasePassword!);
    }

    [Route("taxes")]
    [HttpGet]
    public IActionResult Taxes(
      int page = 1,
      int pageSize = 2)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

      var vm = new TaxesPaginatedViewModel
      {
        Page = page,
        PageSize = pageSize,
        RememberPageSize = string.IsNullOrEmpty(referer),
      };

      return View(vm);
    }

    //using FluentValidation;

    //namespace Accounting.Models.TaxViewModels
    //  {
    //    public class CreateTaxViewModel
    //    {
    //      public string Name { get; set; } = null!;
    //      public string? Description { get; set; }
    //      public decimal Rate { get; set; }
    //      public List<Account>? Accounts { get; set; }
    //      public int SelectedInventoryId { get; set; }
    //      public List<Inventory>? Inventories { get; set; }
    //      public int SelectedAccountId { get; set; }

    //      public class Inventory
    //      {
    //        public int InventoryID { get; set; }
    //        public Item Item { get; set; } = null!;
    //        public Location Location { get; set; } = null!;
    //        public decimal SellFor { get; set; }
    //      }

    //      public class Item
    //      {
    //        public int ItemID { get; set; }
    //        public string Name { get; set; } = null!;
    //      }

    //      public class Location
    //      {
    //        public int LocationID { get; set; }
    //        public string Name { get; set; } = null!;
    //      }

    //      public class Account
    //      {
    //        public int AccountID { get; set; }
    //        public string Name { get; set; } = null!;
    //      }

    //      // required: name, rate, inventoryId, and accountId
    //      public class CreateTaxViewModelValidator : AbstractValidator<CreateTaxViewModel>
    //      {
    //        public CreateTaxViewModelValidator()
    //        {
    //          RuleFor(x => x.Name)
    //            .NotEmpty()
    //            .WithMessage("Name is required.");
    //          RuleFor(x => x.Rate)
    //            .NotEmpty()
    //            .WithMessage("Rate is required.")
    //            .InclusiveBetween(0, 2)
    //            .WithMessage("Rate must be between 0 and 2.");
    //          RuleFor(x => x.SelectedInventoryId)
    //            .NotEmpty()
    //            .WithMessage("InventoryId is required.");
    //          RuleFor(x => x.SelectedAccountId)
    //            .NotEmpty()
    //            .WithMessage("AccountId is required.");
    //        }
    //      }
    //    }
    //  }

    [Route("create")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
      CreateTaxViewModel vm = new();

      List<Account> accounts = await _accountService.GetAllAsync(GetOrganizationId()!.Value, false);

      vm.Accounts = accounts.Select(a => new CreateTaxViewModel.Account
      {
        AccountID = a.AccountID,
        Name = a.Name,
        Type = a.Type
      }).ToList();

      return View(vm);
    }

    [Route("create")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaxViewModel vm)
    {
      CreateTaxViewModel.CreateTaxViewModelValidator validator = new CreateTaxViewModel.CreateTaxViewModelValidator();
      ValidationResult validationResult = await validator.ValidateAsync(vm);

      if (!validationResult.IsValid)
      {

      }

      return RedirectToAction("Taxes");
    }
  }
}