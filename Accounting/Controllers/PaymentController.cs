﻿using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.PaymentViewModels;
using Accounting.Service;
using Accounting.Validators;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("p")]
  public class PaymentController : BaseController
  {
    private readonly JournalService _journalService;  
    private readonly PaymentService _paymentService;
    private readonly InvoiceService _invoiceService;
    private readonly JournalInvoiceInvoiceLineService _journalInvoiceInvoiceLineService;
    private readonly InvoiceInvoiceLinePaymentService _invoiceInvoiceLinePaymentService;
    private readonly InvoiceLineService _invoiceLineService;

    public PaymentController(
      PaymentService paymentService, 
      InvoiceService invoiceService, 
      InvoiceInvoiceLinePaymentService invoiceInvoiceLinePaymentService,
      RequestContext requestContext,
      JournalService journalService,
      InvoiceLineService invoiceLineService)
    {
      _invoiceLineService = new InvoiceLineService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _journalService = new JournalService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _journalInvoiceInvoiceLineService = new JournalInvoiceInvoiceLineService(_invoiceLineService, _journalService, requestContext.DatabaseName, requestContext.DatabasePassword);
      _paymentService = new PaymentService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _invoiceService = new InvoiceService(_journalService, _journalInvoiceInvoiceLineService, requestContext.DatabaseName, requestContext.DatabasePassword);
      _invoiceInvoiceLinePaymentService = new InvoiceInvoiceLinePaymentService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet]
    [Route("void/{id}")]
    public async Task<IActionResult> Void(int id)
    {
      Payment payment = await _paymentService.GetAsync(id, GetOrganizationId()!.Value);

      if (payment == null)
      {
        return NotFound();
      }

      PaymentVoidViewModel model= new PaymentVoidViewModel
      {
        PaymentID = payment.PaymentID,
        ReferenceNumber = payment.ReferenceNumber,
        Amount = payment.Amount
      };

      return View(model);
    }

    [HttpPost]
    [Route("void/{id}")]
    public async Task<IActionResult> Void(PaymentVoidViewModel model)
    {
      PaymentVoidValidator validator = new PaymentVoidValidator();
      ValidationResult validationResult = await validator.ValidateAsync(model);

      Payment payment = await _paymentService.GetAsync(model.PaymentID, GetOrganizationId()!.Value);
      List<Invoice> invoices = await _invoiceInvoiceLinePaymentService.GetAllInvoicesByPaymentIdAsync(model.PaymentID, GetOrganizationId()!.Value);

      if (payment == null)
      {
        return NotFound();
      }

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        model.ReferenceNumber = payment.ReferenceNumber;
        return View(model);
      }

      using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
        await _paymentService.VoidAsync(payment, model.VoidReason, GetUserId(), GetOrganizationId()!.Value);

        foreach (Invoice invoice in invoices)
        {
          await _invoiceService.ComputeAndUpdateInvoiceStatus(invoice.InvoiceID, GetOrganizationId()!.Value);
          await _invoiceService.ComputeAndUpdateTotalAmountAndReceivedAmount(invoice.InvoiceID, GetOrganizationId()!.Value);
          await _invoiceService.UpdateLastUpdated(invoice.InvoiceID, GetOrganizationId()!.Value);
        }

        scope.Complete();
      }

      return RedirectToAction("Invoices", "Invoice");
    }
  }
}