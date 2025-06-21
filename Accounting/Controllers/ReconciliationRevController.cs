using Accounting.CustomAttributes;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [Route("reconciliations")]
  [AuthorizeWithOrganizationId]
  public class ReconciliationRevController 
    : BaseController
  {
    public async Task<IActionResult> Reconciliations()
    {
      return View();
    }
  }
}