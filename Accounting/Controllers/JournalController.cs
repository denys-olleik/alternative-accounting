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
  private readonly InvoiceLineService _invoiceLineService;

  public JournalApiController(RequestContext requestContext)
  {
    _journalService = new JournalService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _invoiceService = new InvoiceService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
    _invoiceLineService = new InvoiceLineService(requestContext.DatabaseName!, requestContext.DatabasePassword!);
  }

  [HttpGet("get-journals")]
  public async Task<IActionResult> GetJournals(
    int page = 1, 
    int pageSize = 10)
  {
    var (journals, nextPage) = await _journalService.GetAllUnionAsync(page, pageSize, GetOrganizationId()!.Value);

    var getJournalsViewModel = new GetJournalsViewModel
    {
      Transactions = journals.Select(j => new GetJournalsViewModel.JournalTransactionViewModel
      {
        JournalTransactionID = j.JournalTransactionID,
        TransactionGuid = j.TransactionGuid,
        LinkType = j.LinkType,
        Created = j.Created
      }).ToList(),
      Page = page,
      NextPage = nextPage,
      PageSize = pageSize,
    };

    foreach (var j in journals)
    {
      switch (j.LinkType)
      {
        case JournalTransaction.LinkTypeConstants.Invoice:
          j.Invoices = new List<Invoice> { await _invoiceService.GetAsync(j.LinkId, GetOrganizationId()!.Value) };
          //j.InvoiceLines = new List<InvoiceLine>() { await _invoiceLineService}
          break;

        case JournalTransaction.LinkTypeConstants.InvoiceLine:
          // TODO: Populate invoice line-related details for j
          break;

        case JournalTransaction.LinkTypeConstants.Payment:
          // TODO: Populate payment-related details for j
          break;

        default:
          // TODO: Handle unknown link type
          break;
      }
    }

    return Ok(getJournalsViewModel);
  }
}