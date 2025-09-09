using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.TaxViewModels;
using Accounting.Service;
using AngleSharp.Css.Values;
using FluentValidation;
using FluentValidation.Results;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("tax")]
  public class TaxController : BaseController
  {
    private readonly AccountService _accountService;
    private readonly ItemService _itemService;
    private readonly LocationService _locationService;
    private readonly TaxService _taxService;

    public TaxController(RequestContext requestContext)
    {
      _accountService = new AccountService(requestContext.DatabaseName!, requestContext!.DatabasePassword!);
      _itemService = new ItemService(requestContext.DatabaseName!, requestContext!.DatabasePassword!);
      _locationService = new LocationService(requestContext.DatabaseName!, requestContext!.DatabasePassword!);
      _taxService = new TaxService(requestContext.DatabaseName!, requestContext!.DatabasePassword!);
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
      var vm = new CreateTaxViewModel();
      await PopulateListsAsync(vm);
      return View(vm);
    }

    [Route("create")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaxViewModel vm)
    {
      var validator = new CreateTaxViewModel.CreateTaxViewModelValidator();
      var validationResult = await validator.ValidateAsync(vm);

      if (!validationResult.IsValid)
      {
        vm.ValidationResult = validationResult;
        await PopulateListsAsync(vm);
        return View(vm);
      }

      await _taxService.CreateAsync(new Tax
      {
        Name = vm.Name,
        Description = vm.Description,
        Rate = vm.Rate,
        ItemId = vm.SelectedItemId!.Value,
        LocationId = vm.SelectedLocationId,
        LiabilityAccountId = vm.SelectedAccountId,
        CreatedById = GetUserId(),
        OrganizationId = GetOrganizationId()!.Value
      });

      return RedirectToAction("Taxes");
    }

    private async System.Threading.Tasks.Task PopulateListsAsync(CreateTaxViewModel vm)
    {
      var orgId = GetOrganizationId()!.Value;

      var accountsTask = _accountService.GetAllAsync(orgId, false);
      var itemsTask = _itemService.GetAllAsync(orgId);
      var locationsTask = _locationService.GetAllAsync(orgId);

      await System.Threading.Tasks.Task.WhenAll(accountsTask, itemsTask, locationsTask);

      vm.Items = itemsTask.Result.Select(i => new CreateTaxViewModel.Item
      {
        ItemID = i.ItemID,
        Name = i.Name
      }).ToList();

      vm.Locations = locationsTask.Result.Select(l => new CreateTaxViewModel.Location
      {
        LocationID = l.LocationID,
        Name = l.Name
      }).ToList();

      vm.Accounts = accountsTask.Result.Select(a => new CreateTaxViewModel.Account
      {
        AccountID = a.AccountID,
        Name = a.Name,
        Type = a.Type
      }).ToList();
    }
  }
}