using Accounting.Common;

namespace Accounting.Business
{
  public class Journal : IIdentifiable<int>
  {
    public int JournalID { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    private decimal? _debit;
    public decimal? Debit
    {
      get => _debit;
      set => _debit = value.HasValue ? Math.Abs(value.Value) : (decimal?)null;
    }

    private decimal? _credit;
    public decimal? Credit
    {
      get => _credit;
      set => _credit = value.HasValue ? Math.Abs(value.Value) : (decimal?)null;
    }
    public string? Memo { get; set; }
    public int CreatedById { get; set; }
    public int OrganizationId { get; set; }
    public DateTime Created { get; set; }

    public int Identifiable => this.JournalID;
  }
}