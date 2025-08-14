namespace Accounting.Models.ToDoViewModels
{
  public class TasksPaginatedViewModel : PaginatedViewModel
  {
    public List<string>? AvailableStatuses { get; set; }
  }
}