﻿using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models;
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
    private readonly AccountService _accountService;

    public ReconciliationRevController(
      RequestContext requestContext)
    {
      _reconciliationTransactionService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationAttachmentService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _accountService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet]
    [Route("reconciliation-transactions/{id}")]
    public async Task<IActionResult> ReconciliationTransactions(
      int id, 
      int page, 
      int pageSize)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

      var reconciliation = await _reconciliationService.GetByIdAsync(id, GetOrganizationId()!.Value);
      var accounts = await _accountService.GetAllAsync(GetOrganizationId()!.Value, false);

      if (reconciliation == null)
        return NotFound();

      reconciliation.ReconciliationAttachment = await _reconciliationAttachmentService.GetByReconciliationIdAsync(reconciliation.ReconciliationID, GetOrganizationId()!.Value);

      var model = new ReconciliationTransactionsPaginatedViewModel
      {
        Page = page,
        PageSize = pageSize,

        Accounts = accounts.Select(a => new ReconciliationTransactionsPaginatedViewModel.AccountViewModel
        {
          AccountID = a.AccountID,
          Name = a.Name,
          Type = a.Type.ToString()
        }).ToList(),

        ReconciliationID = reconciliation.ReconciliationID,
        Name = reconciliation.Name,
        Status = reconciliation.Status,
        OriginalFileName = reconciliation.ReconciliationAttachment?.OriginalFileName,
        RememberPageSize = string.IsNullOrEmpty(referer),
      };

      return View(model);
    }

    public class CsvAnalysisResult
    {
      public bool isCsv { get; set; }
      public int firstDataRow { get; set; }
    }

    public class ReconciliationTransactionResult
    {
      public DateTime TransactionDate { get; set; }
      public string Description { get; set; }
      public decimal Amount { get; set; }
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
          Name = model.Name,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

        string allLines = string.Empty;

        if (model.StatementCsv != null && model.StatementCsv.Length > 0)
        {
          using (var reader = new StreamReader(model.StatementCsv.OpenReadStream()))
          {
            allLines = await reader.ReadToEndAsync();
          }
        }
        else
        {
          allLines = model.StatementCsvText!;
        }

        LanguageModelService languageModelService = new LanguageModelService();
        var (response, structuredResponse) = await languageModelService.GenerateResponse<CsvAnalysisResult>("""
          is this a CSV file and on what line is the first row of data?

          example response:
          {
            "isCsv": true,
            "firstDataRow": 2
          }
          """, string.Join("\n", allLines.Split('\n').Take(10)), false, true);

        if (structuredResponse?.isCsv != true)
          throw new InvalidOperationException("The uploaded file is not a valid CSV file.");

        List<ReconciliationTransaction> transactions = new();
        var lines = allLines.Split('\n');
        for (int i = structuredResponse.firstDataRow - 1; i < lines.Length; i++)
        {
          string row = lines[i].Trim();
          if (string.IsNullOrWhiteSpace(row))
            continue;

          await languageModelService.GenerateResponse<ReconciliationTransactionResult>($"""
            process this CSV row into a ReconciliationTransaction object:
            ```csv
            {row}
            ```

            respond with JSON object that includes TransactionDate, Description, and Amount fields. respond without wrapping in a code block or any other text, so I can parse it easily.
            """,
            string.Empty,
            true,
            true
          ).ContinueWith(t =>
          {
            if (t.Result.structuredResponse != null)
            {
              transactions.Add(new ReconciliationTransaction
              {
                ReconciliationId = reconciliation.ReconciliationID,
                RawData = row,
                TransactionDate = t.Result.structuredResponse.TransactionDate,
                Description = t.Result.structuredResponse.Description,
                Amount = t.Result.structuredResponse.Amount,
                CreatedById = GetUserId(),
                OrganizationId = GetOrganizationId()!.Value,
              });
            }
          });
        }

        foreach (var transaction in transactions)
        { 
          await _reconciliationTransactionService.ImportAsync(transactions);
        }

        scope.Complete();
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

  [AuthorizeWithOrganizationId]
  [ApiController]
  [Route("api/rclrev")]
  public class ReconciliationRevApiController : BaseController
  {
    private readonly ReconciliationTransactionService _reconciliationTransactionService;
    private readonly ReconciliationService _reconciliationService;

    public ReconciliationRevApiController(
      ReconciliationService reconciliationService,
      ReconciliationTransactionService reconciliationTransactionService,
      RequestContext requestContext)
    {
      _reconciliationTransactionService = new ReconciliationTransactionService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationService = new ReconciliationService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [HttpGet("get-reconciliation-transactions")]
    public async Task<IActionResult> GetReconciliationTransactions(
      int reconciliationId,
      int page = 1,
      int pageSize = 2)
    {
      var (transactions, nextPage) = await _reconciliationTransactionService.GetReconciliationTransactionsAsync(reconciliationId, page, pageSize, GetOrganizationId()!.Value);

      GetReconciliationTransactionsViewModel model = new GetReconciliationTransactionsViewModel
      {
        ReconciliationID = reconciliationId,
        Page = page,
        PageSize = pageSize,
        NextPage = nextPage,
        ReconciliationTransactions = transactions.Select(t => new GetReconciliationTransactionsViewModel.ReconciliationTransactionViewModel
        {
          ReconciliationTransactionID = t.ReconciliationTransactionID,
          RowNumber = t.RowNumber,
          TransactionDate = t.TransactionDate,
          Description = t.Description,
          Amount = t.Amount,
          RawData = t.RawData
        }).ToList()
      };

      return Ok(model);
    }

    [HttpGet("get-reconciliations")]
    public async Task<IActionResult> GetReconciliations(int page = 1, int pageSize = 2)
    {
      var (reconciliations, nextPage) = await _reconciliationService.GetAllAsync(page, pageSize, GetOrganizationId()!.Value);

      var getReconciliationsViewModel = new GetReconciliationsViewModel
      {
        Reconciliations = reconciliations.Select(r => new GetReconciliationsViewModel.ReconciliationViewModel
        {
          ReconciliationID = r.ReconciliationID,
          RowNumber = r.RowNumber!.Value,
          Name = r.Name,
          Status = r.Status
        }).ToList(),
        Page = page,
        NextPage = nextPage,
      };

      return Ok(getReconciliationsViewModel);
    }
  }
}