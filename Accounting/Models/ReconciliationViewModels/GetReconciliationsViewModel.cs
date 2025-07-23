using AngleSharp.Css.Values;
using Google.Protobuf.WellKnownTypes;

namespace Accounting.Models.ReconciliationViewModels
{
//-- sudo -i -u postgres psql -d Accounting -c 'SELECT * FROM "Reconciliation";'
//CREATE TABLE "Reconciliation"
//(
//	"ReconciliationID" SERIAL PRIMARY KEY NOT NULL,
//	"Status" VARCHAR(20) CHECK("Status" IN ('pending', 'processed')) DEFAULT 'pending' NOT NULL,
//	"Created" TIMESTAMPTZ NOT NULL DEFAULT(CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
//	"CreatedById" INT NOT NULL,
//	"OrganizationId" INT NOT NULL,
//	FOREIGN KEY("CreatedById") REFERENCES "User"("UserID"),
//	FOREIGN KEY("OrganizationId") REFERENCES "Organization"("OrganizationID")
//);

  public class GetReconciliationsViewModel : PaginatedViewModel
  {
    public List<ReconciliationViewModel>? Reconciliations { get; set; }

    public class ReconciliationViewModel
    {
      public int ReconciliationID { get; set; }
      public string Status { get; set; } 
    }
  }
}