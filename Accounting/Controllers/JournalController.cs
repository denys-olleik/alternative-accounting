using Accounting.Business;
using Accounting.CustomAttributes;
using Microsoft.AspNetCore.Mvc;
using Accounting.Models.JournalViewModels;
using Accounting.Service;

namespace Accounting.Controllers;

[AuthorizeWithOrganizationId]
[Route("journal")]
public class JournalController : BaseController
{
  [Route("journal")]
  public IActionResult Journal(
    int page = 1,
    int pageSize = 2)
  {
    var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

    var vm = new JournalPaginatedViewModel
    {
      Page = page,
      PageSize = pageSize,
      RememberPageSize = string.IsNullOrEmpty(referer),
    };

    return View(vm);
  }
}

[AuthorizeWithOrganizationId]
[ApiController]
[Route("api/journal")]
public class JournalApiController : BaseController
{
  private readonly JournalService _journalService;
  private readonly InvoiceService _invoiceService;
  private readonly JournalInvoiceInvoiceLineService _journalInvoiceInvoiceLineService;
  private readonly JournalInvoiceInvoiceLinePaymentService _journalInvoiceInvoiceLinePaymentService;

  public JournalApiController(RequestContext requestContext)
  {
    _journalService = new JournalService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _invoiceService = new InvoiceService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _journalInvoiceInvoiceLineService = new JournalInvoiceInvoiceLineService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _journalInvoiceInvoiceLinePaymentService = new JournalInvoiceInvoiceLinePaymentService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
  }

  [HttpGet("get-intermediate-details")]
  public async Task<IActionResult> GetIntermediateDetails(string linkType, int id)
  {
    var orgId = GetOrganizationId()!.Value;

    switch ((linkType ?? string.Empty).ToLowerInvariant())
    {
      case "invoice":
        {
          // id = JournalInvoiceInvoiceLineID
          // 1) Resolve junction -> InvoiceId, InvoiceLineId
          var j = await _journalInvoiceInvoiceLineService.GetAsync(id, orgId);
          if (j == null) return NotFound();

          // 2) Load invoice + lines
          var invoice = await _invoiceService.GetByIdAsync(j.InvoiceId, orgId);
          var lines = await _invoiceService.GetLinesByInvoiceIdAsync(j.InvoiceId, orgId);

          // Shape minimal payload for the intermediate details pane
          return Ok(new
          {
            type = "invoice",
            invoice = invoice,      // consider mapping to a DTO
            lines = lines           // consider mapping to a DTO
          });
        }

      case "payment":
        {
          // id = JournalInvoiceInvoiceLinePaymentID
          // 1) Resolve junction -> InvoiceInvoiceLinePaymentId
          var j = await _journalInvoiceInvoiceLinePaymentService.GetByIdAsync(id, orgId);
          if (j == null) return NotFound();

          // 2) Load payment + its allocations (invoices/lines)
          var payment = await _invoiceService.GetPaymentByIdAsync(j.InvoiceInvoiceLinePaymentId, orgId);
          var allocations = await _invoiceService.GetAllocationsByPaymentIdAsync(payment.PaymentID, orgId); // returns list of {InvoiceId, InvoiceLineId, Amount}
                                                                                                            // Optionally hydrate invoice headers for display
          var invoiceIds = allocations.Select(a => a.InvoiceId).Distinct().ToList();
          var invoices = await _invoiceService.GetByIdsAsync(invoiceIds, orgId);

          return Ok(new
          {
            type = "payment",
            payment = payment,
            allocations = allocations,
            invoices = invoices
          });
        }

      case "reconciliation":
        {
          // id = JournalReconciliationTransactionID
          // 1) Resolve junction -> ReconciliationTransactionId
          var j = await _journalService.GetJournalReconciliationByIdAsync(id, orgId);
          if (j == null) return NotFound();

          // 2) Load reconciliation transaction details
          var rt = await _journalService.GetReconciliationTransactionByIdAsync(j.ReconciliationTransactionId, orgId);

          return Ok(new
          {
            type = "reconciliation",
            reconciliationTransaction = rt
          });
        }

      default:
        return BadRequest(new { message = "Unsupported linkType." });
    }
  }

  [HttpGet("get-journals")]
  public async Task<IActionResult> GetJournals(int page = 1, int pageSize = 10)
  {
    var orgId = GetOrganizationId()!.Value;
    var (journalTransactions, nextPage) = await _journalService.GetAllUnionAsync(page, pageSize, orgId);

    var vm = new GetJournalsViewModel
    {
      Transactions = journalTransactions.Select(j => new GetJournalsViewModel.JournalTransactionViewModel
      {
        JournalTransactionID = j.JournalTransactionID,
        TransactionGuid = j.TransactionGuid,
        LinkType = j.LinkType,
        Created = j.Created,
        JournalId = j.JournalId,
        JournalInvoiceInvoiceLineID = j.JournalInvoiceInvoiceLineID,
        JournalInvoiceInvoiceLinePaymentID = j.JournalInvoiceInvoiceLinePaymentID,
        JournalReconciliationTransactionID = j.JournalReconciliationTransactionID
      }).ToList(),
      Page = page,
      NextPage = nextPage,
      PageSize = pageSize,
    };

    return Ok(vm);
  }
}