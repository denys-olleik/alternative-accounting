using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.ReconciliationViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;

namespace Accounting.Controllers
{
  [Route("recrev")]
  [AuthorizeWithOrganizationId]
  public class ReconciliationRevController 
    : BaseController
  {
    private readonly ReconciliationTransactionService _reconciliationTransactionService;
    private readonly ReconciliationService _reconciliationService;
    private readonly ReconciliationAttachmentService _reconciliationAttachmentService;

    public ReconciliationRevController(
      RequestContext requestContext)
    {
      _reconciliationTransactionService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationAttachmentService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet]
    [Route("create")]
    public async Task<IActionResult> Create()
    {
      var model = new CreateReconciliationRevViewModel();
      // Populate any dropdowns or defaults here if needed
      return View(model);
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create(CreateReconciliationRevViewModel model)
    {
      var validator = new CreateReconciliationRevViewModel.CreateReconciliationViewModelValidator();
      var validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        // Repopulate dropdowns or other data if needed
        return View(model);
      }

      // 1. Create Reconciliation
      // 2. Process statement transactions into ReconciliationTransaction
      // 3. Create ReconciliationAttachment
      // 4. Save CSV file to disk
      // have private methods for each step if needed
      using (TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled))
      {
        Reconciliation reconciliation = await _reconciliationService.CreateAsync(new Reconciliation
        {
          StatementType = model.StatementType,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

        string firstTenLinesOfCsv = string.Empty;


      }

      return RedirectToAction("Reconciliations");
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