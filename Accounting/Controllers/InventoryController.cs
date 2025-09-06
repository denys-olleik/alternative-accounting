using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.InventoryViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("inventory")]
  public class InventoryController : BaseController
  {
    private readonly AccountService _accountService;
    private readonly InventoryService _inventoryService;
    private readonly ItemService _itemService;
    private readonly LocationService _locationService;

    public InventoryController(RequestContext requestContext)
    {
      _accountService = new AccountService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
      _inventoryService = new InventoryService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
      _itemService = new ItemService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
      _locationService = new LocationService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    }

    [Route("create/{itemId}")]
    [HttpGet]
    public async Task<IActionResult> Create(int itemId)
    {
      Item? item = await _itemService.GetAsync(itemId, GetOrganizationId()!.Value);
      if (item == null)
      {
        return NotFound();
      }

      CreateInventoryViewModel createInventoryViewModel = new()
      {
        ItemId = itemId,
        Locations = (await _locationService.GetAllAsync(GetOrganizationId()!.Value))
          .Select(l => new CreateInventoryViewModel.LocationViewModel
          {
            LocationID = l.LocationID,
            Name = l.Name
          }).ToList()
      };

      return View();
    }

    [Route("create")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateInventoryViewModel model)
    {
      return View("Create");
    }
  }
}