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