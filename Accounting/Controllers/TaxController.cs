using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.TaxViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;

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

  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/tax")]
  public class TaxApiController : BaseController
  {
    private readonly TaxService _taxService;
    private readonly ItemService _itemService;
    private readonly AccountService _accountService;
    private readonly LocationService _locationService;

    public TaxApiController(RequestContext requestContext)
    {
      _taxService = new TaxService(requestContext.DatabaseName!, requestContext!.DatabasePassword!);
      _itemService = new ItemService(requestContext.DatabaseName!, requestContext!.DatabasePassword!);
      _accountService = new AccountService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
      _locationService = new LocationService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    }

    [Route("get-taxes")]
    [HttpGet]
    public async Task<IActionResult> GetTaxes(
      int page = 1,
      int pageSize = 10)
    {
      var organizationId = GetOrganizationId()!.Value;
      var (taxes, nextPage) = await _taxService.GetAllAsync(page, pageSize, organizationId);

      List<TaxViewModel> taxViewModels = new ();
      foreach (var t in taxes)
      {
        var item = await _itemService.GetAsync(t.ItemId, organizationId);
        var account = await _accountService.GetAsync(t.LiabilityAccountId, organizationId);

        taxViewModels.Add(new TaxViewModel
        {
          TaxID = t.TaxID,
          Name = t.Name,
          Description = t.Description,
          Rate = t.Rate,
          ItemId = t.ItemId,
          Item = new TaxViewModel.ItemViewModel
          {
            ItemID = item.ItemID,
            Name = item.Name!
          },
          LiabilityAccount = new TaxViewModel.AccountViewModel
          {
            AccountID = account.AccountID,
            Name = account.Name!
          },
          LocationId = t.LocationId,
          LiabilityAccountId = t.LiabilityAccountId
        });
      }

      var result = new
      {
        Taxes = taxViewModels,
        NextPage = nextPage
      };

      return Ok(result);
    }
  }
}