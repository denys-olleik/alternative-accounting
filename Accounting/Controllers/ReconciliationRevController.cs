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

//-- sudo -i -u postgres psql -d Accounting -c 'SELECT * FROM "ReconciliationTransaction";'
//CREATE TABLE "ReconciliationTransaction"
//(
//	"ReconciliationTransactionID" SERIAL PRIMARY KEY NOT NULL,
//	"Status" VARCHAR(20) CHECK("Status" IN ('pending', 'processed', 'error')) DEFAULT 'pending' NOT NULL,
//	"RawData" TEXT NULL,
//	"ReconciliationInstruction" VARCHAR(20) NULL CHECK("ReconciliationInstruction" IN ('expense', 'revenue')),
//	"TransactionDate" TIMESTAMPTZ NOT NULL,
//	"Description" VARCHAR(1000) NOT NULL,
//	"Amount" DECIMAL(18, 2) NOT NULL,
//	"ExpenseAccountId" INT NULL,
//	"AssetOrLiabilityAccountId" INT NULL,
//	"Created" TIMESTAMPTZ NOT NULL DEFAULT(CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
//	"ReconciliationId" INT NOT NULL,
//	"CreatedById" INT NOT NULL,
//	"OrganizationId" INT NOT NULL,
//	FOREIGN KEY("ReconciliationId") REFERENCES "Reconciliation"("ReconciliationID"),
//	FOREIGN KEY("ExpenseAccountId") REFERENCES "Account"("AccountID"),
//	FOREIGN KEY("AssetOrLiabilityAccountId") REFERENCES "Account"("AccountID"),
//	FOREIGN KEY("CreatedById") REFERENCES "User"("UserID"),
//	FOREIGN KEY("OrganizationId") REFERENCES "Organization"("OrganizationID")
//);

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
          StatementType = model.StatementType,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

        string allLines = string.Empty;

        // load first 10 lines of CSV file to process transactions
        if (model.StatementCsv != null && model.StatementCsv.Length > 0)
        {
          using (var reader = new StreamReader(model.StatementCsv.OpenReadStream()))
          {
            allLines = await reader.ReadToEndAsync();
          }
        }

        LanguageModelService languageModelService = new LanguageModelService();
        var (response, structuredResponse) = await languageModelService.GenerateResponse<dynamic>("""
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
        // Process the CSV file to extract transactions, make sure to offset the first data row
        for (int i = structuredResponse.firstDataRow - 1; i < allLines.Split('\n').Length; i++)
        {
          string row = allLines.Split('\n')[i].Trim();

          await languageModelService.GenerateResponse<dynamic>($"""
            process this CSV row into a ReconciliationTransaction object:
            {row}
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
                RawData = t.Result.structuredResponse.RawData,
                ReconciliationInstruction = model.StatementType,
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