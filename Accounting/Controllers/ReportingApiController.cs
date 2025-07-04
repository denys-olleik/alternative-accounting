﻿using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/r")]
  public class ReportingApiController : BaseController
  {
    private readonly InvoiceService _invoiceService;
    private readonly AccountService _accountService;
    private readonly JournalService _journalService;
    private readonly JournalInvoiceInvoiceLineService _journalInvoiceInvoiceLineService;
    private readonly InvoiceLineService _invoiceLineService;

    public ReportingApiController(
      RequestContext requestContext, 
      InvoiceLineService invoiceLineServie,
      JournalInvoiceInvoiceLineService journalInvoiceInvoiceLineService,
      JournalService journalService,
      InvoiceLineService invoiceLineService)
    {
      _journalService = new JournalService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _invoiceLineService = new InvoiceLineService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _journalInvoiceInvoiceLineService = new JournalInvoiceInvoiceLineService(_invoiceLineService, _journalService, requestContext.DatabaseName, requestContext.DatabasePassword);
      _invoiceService = new InvoiceService(_journalService, _journalInvoiceInvoiceLineService, requestContext.DatabaseName, requestContext.DatabasePassword);
      _accountService = new AccountService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet("get-unpaid-and-paid-balance")]
    public async Task<IActionResult> GetUnpaidAndPaidBalance()
    {
      (decimal unpaid, decimal paid) = await _invoiceService.GetUnpaidAndPaidBalance(GetOrganizationId()!.Value);

      return Ok(new { unpaid, paid });
    }

    [HttpGet("get-account-balance-report")]
    public async Task<IActionResult> GetAccountBalanceReport()
    {
      var accountBalances = await _accountService.GetAccountBalanceReport(GetOrganizationId()!.Value);

      return Ok(accountBalances);
    }
  }
}