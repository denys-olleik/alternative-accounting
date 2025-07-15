using FluentValidation;
using FluentValidation.Results;

namespace Accounting.Models.ReconciliationViewModels
{
//-- sudo -i -u postgres psql -d Accounting -c 'SELECT * FROM "Reconciliation";'
//CREATE TABLE "Reconciliation"
//(
//	"ReconciliationID" SERIAL PRIMARY KEY NOT NULL,
//	"Status" VARCHAR(20) CHECK("Status" IN ('pending', 'processed')) DEFAULT 'pending' NOT NULL,
//	"StatementType" VARCHAR(20) CHECK("StatementType" IN ('bank', 'credit-card')) NULL,
//	"Created" TIMESTAMPTZ NOT NULL DEFAULT(CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
//	"CreatedById" INT NOT NULL,
//	"OrganizationId" INT NOT NULL,
//	FOREIGN KEY("CreatedById") REFERENCES "User"("UserID"),
//	FOREIGN KEY("OrganizationId") REFERENCES "Organization"("OrganizationID")
//);

//-- sudo -i -u postgres psql -d Accounting -c 'SELECT * FROM "ReconciliationAttachment";'
//CREATE TABLE "ReconciliationAttachment"
//(
//	"ReconciliationAttachmentID" SERIAL PRIMARY KEY NOT NULL,
//	"OriginalFileName" VARCHAR(255) NOT NULL,
//	"FilePath" VARCHAR(1000) NOT NULL,
//	"Created" TIMESTAMPTZ NOT NULL DEFAULT(CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
//	"ReconciliationId" INT NULL, -- Attachment can be uploaded before reconciliation creation
//	"CreatedById" INT NOT NULL,
//	"OrganizationId" INT NOT NULL,
//	FOREIGN KEY("ReconciliationId") REFERENCES "Reconciliation"("ReconciliationID"),
//	FOREIGN KEY("CreatedById") REFERENCES "User"("UserID"),
//	FOREIGN KEY("OrganizationId") REFERENCES "Organization"("OrganizationID"),
//	UNIQUE("ReconciliationId", "OrganizationId")
//);

//-- sudo -i -u postgres psql -d Accounting -c 'SELECT * FROM "ReconciliationTransaction";'
//CREATE TABLE "ReconciliationTransaction"
//(
//	"ReconciliationTransactionID" SERIAL PRIMARY KEY NOT NULL,
//	"Status" VARCHAR(20) CHECK("Status" IN ('pending', 'processed', 'error')) DEFAULT 'pending' NOT NULL,
//	"RawData" TEXT NULL,
//	"ReconciliationInstruction" VARCHAR(20) NULL CHECK("ReconciliationInstruction" IN ('expense', 'revenue')),
//	"TransactionDate" TIMESTAMPTZ NOT NULL,
//	"PostedDate" TIMESTAMPTZ NOT NULL,
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

  public class CreateReconciliationRevViewModel 
  {
    public int? ReconciliationID { get; set; }
    public string? StatementType { get; set; }

    public IFormFile? StatementCsv { get; set; }

    public ValidationResult? ValidationResult { get; set; }

    public class CreateReconciliationViewModelValidator : AbstractValidator<CreateReconciliationRevViewModel>
    {
      public CreateReconciliationViewModelValidator()
      {
        RuleFor(x => x.StatementType)
          .NotEmpty().WithMessage("Statement type is required.")
          .Must(value => value == "bank" || value == "credit-card")
          .WithMessage("Invalid statement type.");
        RuleFor(x => x.StatementCsv)
          .Must(file => file != null && file.Length > 0).WithMessage("CSV file cannot be empty.");
      }
    }
  }
}