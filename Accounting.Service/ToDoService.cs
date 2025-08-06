using Accounting.Business;
using Accounting.Database;

namespace Accounting.Service
{
  public class ToDoService : BaseService
  {
    public ToDoService() : base()
    {
      
    }

    public ToDoService(
      string databaseName,
      string databasePassword) : base(databaseName, databasePassword)
    {

    }

    public async Task<Business.Task> CreateAsync(Business.Task taskItem)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetTaskManager().CreateAsync(taskItem);
    }

    public async Task<List<Business.Task>> GetAllAsync(int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      List<Business.Task> taskItems = await factoryManager.GetTaskManager().GetAllAsync(organizationId);

      List<Business.Task> rootTasks = taskItems.Where(t => t.ParentToDoId == null).ToList();

      foreach (Business.Task rootTask in rootTasks)
      {
        BuildTree(taskItems, rootTask);
      }

      return rootTasks;
    }

    public async Task<List<Business.Task>> GetChildrenAsync(int parentId, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetTaskManager().GetChildrenAsync(parentId, organizationId);
    }

    private void BuildTree(List<Business.Task> allTasks, Business.Task parentTask)
    {
      List<Business.Task> children = allTasks.Where(t => t.ParentToDoId == parentTask.ToDoID).ToList();

      foreach (Business.Task child in children)
      {
        BuildTree(allTasks, child);
        parentTask.Children.Add(child);
      }
    }

    public async Task<Business.Task> GetAsync(int id, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetTaskManager().GetAsync(id, organizationId);
    }

    public async Task<Business.Task> UpdateContentAsync(int taskId, string content, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetTaskManager().UpdateContentAsync(taskId, content, organizationId);
    }

    public async Task<int> UpdateParentTaskIdAsync(int taskId, int? newParentId, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetTaskManager().UpdateParentToDoIdAsync(taskId, newParentId, organizationId);
    }

    public async Task<List<Business.Task>> GetTaskChildren(int id, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      Business.Task rootTask = await GetAsync(id, organizationId);
      List<Business.Task> descendants = await factoryManager.GetTaskManager().GetDescendantsAsync(id, organizationId);
      BuildTree(descendants, rootTask);
      return rootTask.Children;
    }

    public async Task<int> UpdateTaskStatusIdAsync(int taskId, string status, int organizationId)
    {
      var factoryManager = new FactoryManager(_databaseName, _databasePassword);
      return await factoryManager.GetTaskManager().UpdateTaskStatusIdAsync(taskId, status, organizationId);
    }
  }
}
