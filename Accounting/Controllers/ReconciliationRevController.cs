using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.ReconciliationViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;
using static Accounting.Models.ReconciliationViewModels.ReconciliationsViewModel;

namespace Accounting.Controllers
{
  [Route("reconciliations")]
  [AuthorizeWithOrganizationId]
  public class ReconciliationRevController 
    : BaseController
  {
    private readonly ReconciliationService _reconciliationService;

    public ReconciliationRevController(
      RequestContext requestContext)
    {
      _reconciliationService = new (requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [Route("reconciliationsrev")]
    [HttpGet]
    public async Task<IActionResult> Reconciliations(
      int page = 1,
      int pageSize = 2)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

      var vm = new ReconciliationsPaginatedViewModel
      {
        Page = page,
        PageSize = pageSize,
        RememberPageSize = string.IsNullOrEmpty(referer),
      };

      return View(vm);
    }
  }
}