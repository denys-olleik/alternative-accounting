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

    public InventoryController(RequestContext requestContext)
    {
      _accountService = new AccountService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    }

    [Route("create")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {


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