
namespace Accounting.Database.Interfaces
{
  public interface IToDoManager : IGenericRepository<Business.Task, int>
  {
    System.Threading.Tasks.Task<List<Business.Task>> GetAllAsync(int organizationId);
    System.Threading.Tasks.Task<Business.Task> GetAsync(int id, int organizationId);
    System.Threading.Tasks.Task<List<Business.Task>> GetChildrenAsync(int parentId, int organizationId);
    System.Threading.Tasks.Task<List<Business.Task>> GetDescendantsAsync(int id, int organizationId);
    System.Threading.Tasks.Task<List<Business.Task>> GetToDoChildrenAsync(int id, int organizationId);
    System.Threading.Tasks.Task<Business.Task> UpdateContentAsync(int taskId, string content, int organizationId);
    System.Threading.Tasks.Task<int> UpdateParentToDoIdAsync(int taskId, int? newParentId, int organizationId);
    System.Threading.Tasks.Task<int> UpdateTaskStatusIdAsync(int taskId, string status, int organizationId);
  }
}