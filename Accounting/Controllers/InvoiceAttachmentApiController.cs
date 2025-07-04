﻿using Accounting.Business;
using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Service;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/invoice-attachment")]
  public class InvoiceAttachmentApiController : BaseController
  {
    private readonly InvoiceAttachmentService _invoiceAttachmentService;

    public InvoiceAttachmentApiController(RequestContext requestContext, InvoiceAttachmentService invoiceAttachmentService)
    {
      _invoiceAttachmentService = new InvoiceAttachmentService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet("download/{invoiceAttachmentId}")]
    public async Task<IActionResult> Download(int invoiceAttachmentId)
    {
      InvoiceAttachment attachment = await _invoiceAttachmentService.GetInvoiceAttachmentAsync(invoiceAttachmentId, GetOrganizationId()!.Value);
      if (attachment == null)
      {
        return NotFound();
      }

      var fileBytes = await _invoiceAttachmentService.GetAttachmentFileBytesAsync(attachment);
      var contentType = "application/octet-stream";
      var fileName = attachment.OriginalFileName ?? "attachment";

      return File(fileBytes, contentType, fileName);
    }

    [Route("upload")]
    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile formFile)
    {
      Common.File fileUpload = new Common.File
      {
        FileName = formFile.FileName,
        Stream = formFile.OpenReadStream()
      };

      InvoiceAttachment attachment = await _invoiceAttachmentService.UploadInvoiceAttachmentAsync(fileUpload, GetUserId(), GetOrganizationId()!.Value, GetDatabaseName());

      return Ok(new { InvoiceAttachmentID = attachment.InvoiceAttachmentID, FileName = attachment.OriginalFileName });
    }

    [Route("update-print-order")]
    [HttpPost]
    public async Task<IActionResult> UpdatePrintOrder([FromBody] UpdatePrintOrderModel model)
    {
      bool isSuccess = await _invoiceAttachmentService.UpdatePrintOrderAsync(model.ID, model.NewPrintOrder, GetUserId(), GetOrganizationId()!.Value);

      if (isSuccess)
      {
        return Ok(new { Status = "Success", Message = "Print order updated successfully." });
      }
      else
      {
        return BadRequest(new { Status = "Failed", Message = "Failed to update print order." });
      }
    }
  }

  public class UpdatePrintOrderModel
  {
    public int ID { get; set; }
    public int NewPrintOrder { get; set; }
  }
}