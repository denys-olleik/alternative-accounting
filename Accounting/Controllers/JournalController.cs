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

  public JournalApiController(RequestContext requestContext)
  {
    _journalService = new JournalService(requestContext.DatabaseName, requestContext.DatabasePassword);
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
      }).ToList()
    };

    return Ok(getJournalsViewModel);
  }
}