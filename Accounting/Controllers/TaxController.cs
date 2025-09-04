using Accounting.CustomAttributes;
using Accounting.Models.TaxViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("tax")]
  public class TaxController : BaseController
  {
    public TaxController()
    {

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
    public IActionResult Create()
    {
      return View();
    }

    [Route("create")]
    [HttpPost]
    public IActionResult Create(CreateTaxViewModel vm)
    {


      return RedirectToAction("Taxes");
    }
  }
}