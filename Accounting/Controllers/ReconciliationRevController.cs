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
    public async Task<IActionResult> Reconciliations()
    {
      List<Reconciliation> reconciliations
          = await _reconciliationService.GetAllDescendingAsync(20, GetOrganizationId()!.Value);

      var model = new ReconciliationsViewModel();
      model.Reconciliations = new List<ReconciliationViewModel>();

      foreach (var reconciliation in reconciliations)
      {
        var reconciliationViewModel = new ReconciliationViewModel
        {
          ID = reconciliation.ReconciliationID,
          Status = reconciliation.Status,
          OriginalFileName = Path.GetFileName(reconciliation.ReconciliationAttachment.OriginalFileName),
          Created = reconciliation.Created,
          CreatedById = reconciliation.CreatedById,
          OrganizationId = reconciliation.OrganizationId
        };

        model.Reconciliations.Add(reconciliationViewModel);
      }

      return View(model);
    }
  }
}