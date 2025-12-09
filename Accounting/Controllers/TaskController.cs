using Accounting.Business;
using Accounting.CustomAttributes;
using Accounting.Models.TagViewModels;
using Accounting.Models.ToDoViewModels;
using Accounting.Models.UserViewModels;
using Accounting.Service;
using Accounting.Validators;
using FluentValidation.Results;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Transactions;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("t")]
  public class TaskController : BaseController
  {
    private readonly TagService _tagService;
    private readonly UserService _userService;
    private readonly UserTaskService _userTaskService;
    private readonly ToDoService _toDoService;
    private readonly ToDoTagService _toDoTagService;

    public TaskController(
      RequestContext requestContext, 
      TagService tagService, 
      UserService userService, 
      UserTaskService userTaskService, 
      ToDoService toDoService, 
      ToDoTagService toDoTagService)
    {
      _tagService = new TagService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _userService = new UserService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _userTaskService = new UserTaskService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _toDoService = new ToDoService(requestContext.DatabaseName, requestContext.DatabasePassword);
      _toDoTagService = new ToDoTagService(requestContext.DatabaseName, requestContext.DatabasePassword);
    }

    [Route("tasks")]
    [HttpGet]
    public async Task<IActionResult> Tasks()
    {
      TasksPaginatedViewModel vm = new TasksPaginatedViewModel();
      vm.AvailableStatuses = Business.Task.TaskStatusConstants.All.ToList();

      return View(vm);
    }

    [Route("tasks-rev")]
    [HttpGet]
    public async Task<IActionResult> TasksRev()
    {
      TasksPaginatedViewModel vm = new ();
      vm.AvailableStatuses = Business.Task.TaskStatusConstants.All.ToList();
      return View("Tasks", vm);
    }

    [HttpGet]
    [Route("create")]
    public async Task<IActionResult> Create(int? parentToDoId)
    {
      List<Tag> tags = await _tagService.GetAllAsync();

      CreateTaskViewModel createToDoViewModel = new CreateTaskViewModel();
      createToDoViewModel.Users = (await _userService.GetAllAsync(GetOrganizationId()!.Value)).Select(user => new UserViewModel
      {
        UserID = user.UserID,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName
      }).ToList();

      createToDoViewModel.ParentToDoId = parentToDoId;

      Business.Task? toDo = parentToDoId.HasValue ? await _toDoService.GetAsync(parentToDoId.Value, GetOrganizationId()!.Value) : null;
      createToDoViewModel.ParentToDo = toDo != null ? new ToDoViewModel
      {
        ToDoID = toDo.ToDoID,
        Title = toDo.Title,
        HtmlContent = toDo.Content
      } : null;

      createToDoViewModel.ToDoStatuses = Business.Task.TaskStatusConstants.All.Select(s => s.ToLower()).ToList();

      return View(createToDoViewModel);
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create(CreateTaskViewModel model)
    {
      CreateTaskViewModelValidator validator = new CreateTaskViewModelValidator();
      ValidationResult validationResult = await validator.ValidateAsync(model);

      var deserializedSelectedTagIds = JsonConvert.DeserializeObject<List<int>>(model.SelectedTagIds!);
      model.SelectedUsers = JsonConvert.DeserializeObject<List<int>>(model.SelectedUsersJson!);

      Business.Task? parentToDoItem = model.ParentToDoId.HasValue ? await _toDoService.GetAsync(model.ParentToDoId.Value, GetOrganizationId()!.Value) : null;
      model.ParentToDo = parentToDoItem != null ? new ToDoViewModel
      {
        ToDoID = parentToDoItem.ToDoID,
        Title = parentToDoItem.Title,
        HtmlContent = parentToDoItem.Content
      } : null;

      if (!validationResult.IsValid)
      {
        List<Tag> tags = await _tagService.GetAllAsync();

        if (deserializedSelectedTagIds != null && deserializedSelectedTagIds.Any())
        {
          model.SelectedTags = tags.Where(tag => deserializedSelectedTagIds.Contains(tag.TagID))
              .Select(tag => new TagViewModel
              {
                ID = tag.TagID,
                Name = tag.Name
              }).ToList();

          tags = tags.Where(tag => !deserializedSelectedTagIds.Contains(tag.TagID)).ToList();
        }

        model.ParentToDoId = model.ParentToDoId;
        model.ToDoStatuses = Business.Task.TaskStatusConstants.All.Select(s => s.ToLower()).ToList();

        model.Users = (await _userService.GetAllAsync(GetOrganizationId()!.Value)).Select(user => new UserViewModel
        {
          UserID = user.UserID,
          Email = user.Email,
          FirstName = user.FirstName,
          LastName = user.LastName
        }).ToList();

        model.ValidationResult = validationResult;
        return View(model);
      }

      using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
        Business.Task taskItem = await _toDoService.CreateAsync(new Business.Task()
        {
          Title = model.Title,
          Content = model.Content,
          ParentToDoId = model.ParentToDoId,
          Status = Business.Task.TaskStatusConstants.Open,
          CreatedById = GetUserId(),
          OrganizationId = GetOrganizationId()!.Value
        });

        if (model.SelectedUsers != null && model.SelectedUsers.Any())
        {
          foreach (int userId in model.SelectedUsers)
          {
            UserToDo userTask = new UserToDo();
            userTask.UserId = userId;
            userTask.ToDoId = taskItem.ToDoID;
            userTask.Completed = false;
            userTask.OrganizationId = GetOrganizationId()!.Value;
            userTask.CreatedById = GetUserId();
            await _userTaskService.CreateAsync(userTask);
          }
        }

        if (deserializedSelectedTagIds != null && deserializedSelectedTagIds.Any())
        {
          foreach (int tagId in deserializedSelectedTagIds)
          {
            ToDoTag taskTag = new ToDoTag();
            taskTag.TaskId = taskItem.ToDoID;
            taskTag.TagId = tagId;
            taskTag.OrganizationId = GetOrganizationId()!.Value;
            await _toDoTagService.CreateAsync(taskTag);
          }
        }

        scope.Complete();
      }

      return RedirectToAction("Tasks");
    }

    [HttpGet]
    [Route("details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
      Business.Task taskItem = await _toDoService.GetAsync(id, GetOrganizationId()!.Value);

      MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

      ToDoViewModel taskViewModel = await ConvertToTaskViewModel(taskItem, _toDoService, pipeline);

      return View(taskViewModel);
    }

    private async Task<ToDoViewModel> ConvertToTaskViewModel(Business.Task task, ToDoService taskService, MarkdownPipeline pipeline)
    {
      var taskViewModel = new ToDoViewModel()
      {
        ToDoID = task.ToDoID,
        Title = task.Title,
        HtmlContent = task.Content != null ? Markdown.ToHtml(task.Content, pipeline) : string.Empty,
        MarkdownContent = task.Content,
        ParentToDoId = task.ParentToDoId,
        Created = task.Created,
        Children = new List<ToDoViewModel>()
      };

      var children = await taskService.GetChildrenAsync(task.ToDoID, GetOrganizationId()!.Value);
      foreach (var child in children)
      {
        taskViewModel.Children.Add(await ConvertToTaskViewModel(child, taskService, pipeline));
      }

      return taskViewModel;
    }
  }
}