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

    foreach (var jt in journalTransactions)
    {
      switch (jt.Type)
      {
        case JournalTransaction.LinkTypeConstants.Invoice:
          jt.Invoices = new List<Invoice> { await _invoiceService.GetAsync(jt.JournalTransactionID, orgId) };
          jt.JournalsForInvoice = await _journalInvoiceInvoiceLineService.GetByInvoiceIdAsync(jt.JournalTransactionID, orgId);
          jt.InvoiceLines = await _journalInvoiceInvoiceLineService.GetByInvoiceIdAsync(jt.JournalTransactionID, orgId, false);
          break;

        case JournalTransaction.LinkTypeConstants.Payment:
          // TODO: Populate payment-related details and journals
          // jt.Invoices = ...
          // jt.Payments = ...
          jt.JournalsForPayment = await _journalInvoiceInvoiceLinePaymentService.GetByPaymentIdAsync(jt.JournalTransactionID, orgId);
          break;

        case JournalTransaction.LinkTypeConstants.Reconciliation:
          // TODO: Populate reconciliation-related details and journals
          // jt.ReconciliationTransactions = ...
          // jt.Journals = await _journalService.GetByTransactionGuid(jt.TransactionGuid, orgId);
          break;

        default:
          // TODO: Handle unknown link type if needed
          break;
      }
    }

    var vm = new GetJournalsViewModel
    {
      Transactions = journalTransactions.Select(j => new GetJournalsViewModel.JournalTransactionViewModel
      {
        JournalTransactionID = j.JournalTransactionID,
        TransactionGuid = j.TransactionGuid,
        LinkType = j.Type,
        Created = j.Created,
        Journals = j.JournalsForInvoice?.Select(x => new GetJournalsViewModel.JournalViewModel
        {
          JournalID = x.Journal!.JournalID,
          //AccountId = x.AccountId,
          Debit = x.Journal.Debit,
          Credit = x.Journal.Credit,
          //CurrencyCode = x.CurrencyCode,
          //ExchangeRate = x.ExchangeRate
        }).ToList()!
      }).ToList(),
      Page = page,
      NextPage = nextPage,
      PageSize = pageSize,
    };

    return Ok(vm);
  }
}