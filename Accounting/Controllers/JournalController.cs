using Accounting.Business;
using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Models.JournalViewModels;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;

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
  private readonly BusinessEntityService _businessEntityService;
  private readonly JournalService _journalService;
  private readonly AccountService _accountService;
  private readonly InvoiceService _invoiceService;
  private readonly PaymentService _paymentService;
  private readonly InvoiceInvoiceLinePaymentService _invoiceInvoiceLinePaymentService;
  private readonly JournalInvoiceInvoiceLineService _journalInvoiceInvoiceLineService;
  private readonly JournalInvoiceInvoiceLinePaymentService _journalInvoiceInvoiceLinePaymentService;
  private readonly JournalReconciliationTransactionService _journalReconciliationTransactionService;
  private readonly ReconciliationTransactionService _reconciliationTransactionService;


  public JournalApiController(RequestContext requestContext)
  {
    _businessEntityService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _journalService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _accountService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _invoiceService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _paymentService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _journalInvoiceInvoiceLineService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _journalInvoiceInvoiceLinePaymentService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _reconciliationTransactionService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _invoiceInvoiceLinePaymentService = new(requestContext.DatabaseName!, requestContext.DatabasePassword!);
  }

  [HttpGet("get-details")]
  public async Task<IActionResult> GetIntermediateDetails(string linkType, int id)
  {
    var orgId = GetOrganizationId()!.Value;

    switch ((linkType ?? string.Empty).ToLowerInvariant())
    {
      case "invoice":
        {
          var journalTransaction = await _journalInvoiceInvoiceLineService.GetAsync(id, orgId);
          if (journalTransaction == null) return NotFound();

          var invoice = await _invoiceService.GetAsync(journalTransaction.InvoiceId, orgId);
          invoice.BusinessEntity = await _businessEntityService.GetAsync(invoice.BusinessEntityId, orgId);
          var lines = await _journalInvoiceInvoiceLineService.GetByInvoiceIdAsync(journalTransaction.InvoiceId, orgId, false);
          List<Journal> journal = await _journalService.GetByTransactionGuid(FeaturesIntegratedJournalConstants.JournalInvoiceInvoiceLine, journalTransaction.TransactionGuid, orgId);

          foreach (var j in journal)
          {
            j.Account = await _accountService.GetAsync(j.AccountId, orgId);
          }

          return Ok(new
          {
            type = "invoice",
            invoice = invoice,      // consider mapping to a DTO
            lines = lines,
            journal = journal
          });
        }

      case "payment":
        {
          var journalTransaction = await _journalInvoiceInvoiceLineService.GetAsync(id, orgId);
          var invoiceInvoiceLinePayment = await _invoiceInvoiceLinePaymentService.GetByJournalInvoiceInvoiceLinePaymentIdAsync(id, orgId);
          if (invoiceInvoiceLinePayment == null) return NotFound();

          var payment = await _paymentService.GetAsync(invoiceInvoiceLinePayment.PaymentId, orgId);
          var invoices = await _invoiceService.GetByJournalInvoiceInvoiceLinePaymentIdAsync(id, orgId);
          List<Journal> journal = await _journalService.GetByTransactionGuid(FeaturesIntegratedJournalConstants.JournalInvoiceInvoiceLinePayment, journalTransaction.TransactionGuid, orgId);

          return Ok(new
          {
            type = "payment",
            payment = invoiceInvoiceLinePayment.Payment,
            invoices = invoices,
            journal = journal
          });
        }

      case "reconciliation":
        {
          var journalTransaction = await _journalReconciliationTransactionService.GetAsync(id, orgId);
          if (journalTransaction == null) return NotFound();

          var reconciliationTransaction = await _reconciliationTransactionService.GetAsync(journalTransaction.ReconciliationTransactionId, orgId);

          return Ok(new
          {
            type = "reconciliation",
            reconciliationTransaction = reconciliationTransaction
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