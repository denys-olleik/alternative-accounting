using Accounting.Business;
using Accounting.CustomAttributes;
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

    public ReconciliationRevController(
      RequestContext requestContext)
    {
      _reconciliationTransactionService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
      _reconciliationAttachmentService = new(requestContext.DatabaseName, requestContext.DatabasePassword);
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

          await languageModelService.GenerateResponse<ReconciliationTransactionResult>($$"""
            process this CSV row into a ReconciliationTransaction object:
            ```csv
            {{row}}
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
                RawData = row,
                TransactionDate = t.Result.structuredResponse.TransactionDate,
                Description = t.Result.structuredResponse.Description,
                Amount = t.Result.structuredResponse.Amount,
              });
            }
          });
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
}