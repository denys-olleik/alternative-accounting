using Accounting.Business;
using Accounting.Common;
using Accounting.CustomAttributes;
using Accounting.Models;
using Accounting.Models.BlogViewModels;
using Accounting.Service;
using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Transactions;
using static Accounting.Models.BlogViewModels.CreateBlogViewModel;

namespace Accounting.Controllers
{
  [AuthorizeWithOrganizationId]
  [Route("blog")]
  public class BlogController : BaseController
  {
    private readonly BlogService _blogService;
    private readonly BlogAttachmentService _blogAttachmentService;

    public BlogController(RequestContext requestContext, BlogService blogService)
    {
      _blogService = new BlogService(
        requestContext.DatabaseName,
        requestContext.DatabasePassword);

      _blogAttachmentService = new BlogAttachmentService(
        requestContext.DatabaseName,
        requestContext.DatabasePassword);
    }

    [HttpGet("update/{blogID}")]
    public async Task<IActionResult> Update(int blogID)
    {
      Blog blog = await _blogService.GetAsync(blogID);

      if (blog == null)
      {
        return NotFound();
      }

      var updateBlogViewModel = new UpdateBlogViewModel
      {
        BlogID = blog.BlogID,
        Title = blog.Title,
        Content = blog.Content,
        Public = !string.IsNullOrEmpty(blog.PublicId),
        Attachments = (await _blogAttachmentService.GetAllAsync(new[] { blogID }, GetOrganizationId()!.Value))
          .Select(a => (IAttachmentViewModel)new BlogAttachmentViewModel
          {
            BlogAttachmentID = a.BlogAttachmentID,
            FileName = a.OriginalFileName,
            TranscodeStatusJSONB = a.TranscodeStatusJSONB,
            TranscodeStatusByVariant = string.IsNullOrWhiteSpace(a.TranscodeStatusJSONB)
              ? new Dictionary<string, TranscodeStatus>()
              : JsonConvert.DeserializeObject<Dictionary<string, TranscodeStatus>>(a.TranscodeStatusJSONB)
          })
          .ToList()
      };

      return View(updateBlogViewModel);
    }

    [HttpPost("update/{blogID}")]
    public async Task<IActionResult> Update(UpdateBlogViewModel model)
    {
      var validator = new UpdateBlogViewModel.UpdateBlogViewModelValidator();
      var validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        return View(model);
      }

      var blog = new Blog
      {
        BlogID = model.BlogID,
        Title = model.Title,
        Content = model.Content
      };

      if (model.Public)
      {
        blog.PublicId = RandomHelper.GenerateSecureAlphanumericString(10, true);
      }

      using (TransactionScope scope = new (TransactionScopeAsyncFlowOption.Enabled))
      {
        if (!string.IsNullOrEmpty(model.DeletedAttachmentIdsCsv))
        {
          var ids = model.DeletedAttachmentIdsCsv
              .Split(',', StringSplitOptions.RemoveEmptyEntries)
              .Select(id => int.Parse(id.Trim()))
              .ToList();
          await _blogAttachmentService.DeleteAttachmentsAsync(ids, blog.BlogID, GetOrganizationId()!.Value);
        }

        if (!string.IsNullOrEmpty(model.NewAttachmentIdsCsv))
        {
          var newAttachmentIds = model.NewAttachmentIdsCsv
              .Split(',', StringSplitOptions.RemoveEmptyEntries)
              .Select(id => int.Parse(id.Trim()))
              .ToList();

          var newAttachments = await _blogAttachmentService.GetAllAsync(newAttachmentIds.ToArray(), GetOrganizationId()!.Value);

          foreach (var attachment in newAttachments)
          {
            await _blogAttachmentService.UpdateBlogIdAsync(attachment.BlogAttachmentID, blog.BlogID, GetOrganizationId()!.Value);
            // Optionally move file or update path if needed
            await _blogAttachmentService.MoveAndUpdateBlogAttachmentPathAsync(attachment, ConfigurationSingleton.Instance.PermPath, GetOrganizationId()!.Value, GetDatabaseName());
          }
        }

        await _blogService.UpdateAsync(blog);

        scope.Complete();
      }
      
      return RedirectToAction("Blogs");
    }

    [AllowAnonymous]
    [HttpGet("view/{id}")]
    public async Task<IActionResult> View(string id)
    {
      Blog blog = null;

      if (int.TryParse(id, out var numericId))
      {
        blog = await _blogService.GetAsync(numericId);
      }
      else
      {
        blog = await _blogService.GetByPublicIdAsync(id);
      }

      if (blog == null)
      {
        return NotFound();
      }

      var markdownPipeline = new MarkdownPipelineBuilder().Build();

      var viewBlogViewModel = new ViewBlogViewModel
      {
        BlogID = blog.BlogID,
        PublicId = blog.PublicId,
        Title = blog.Title,
        Content = blog.Content,
        ContentHtml = new HtmlSanitizer().Sanitize(Markdown.ToHtml(blog.Content, markdownPipeline))
      };

      return View(viewBlogViewModel);
    }


    [HttpGet("delete/{blogID}")]
    public async Task<IActionResult> Delete(int blogID)
    {
      Blog blog = await _blogService.GetAsync(blogID);

      if (blog == null)
      {
        return NotFound();
      }

      var deleteBlogViewModel = new DeleteBlogViewModel
      {
        BlogID = blog.BlogID,
        Title = blog.Title,
        Content = blog.Content
      };

      return View(deleteBlogViewModel);
    }

    [HttpPost("delete/{blogID}")]
    public async Task<IActionResult> Delete(DeleteBlogViewModel model)
    {
      await _blogService.DeleteAsync(model.BlogID);
      return RedirectToAction("Blogs");
    }

    [Route("create")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
      return View();
    }

    [Route("create")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateBlogViewModel model)
    {
      model.BlogAttachments = JsonConvert.DeserializeObject<List<BlogAttachmentViewModel>>(model.BlogAttachmentsJSON ?? "[]");

      var validator = new CreateBlogViewModel.CreateBlogViewModelValidator();
      var validationResult = await validator.ValidateAsync(model);

      if (!validationResult.IsValid)
      {
        model.ValidationResult = validationResult;
        return View(model);
      }

      var blog = new Blog
      {
        Title = model.Title,
        Content = model.Content,
        CreatedById = GetUserId(),
      };

      if (model.Public)
      {
        blog.PublicId = RandomHelper.GenerateSecureAlphanumericString(10, true);
      }

      using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
      {
        blog = await _blogService.CreateAsync(blog);

        var blogAttachments = await _blogAttachmentService.GetAllAsync(
          model.BlogAttachments.Select(x => x.BlogAttachmentID).ToArray(),
          GetOrganizationId()!.Value);

        foreach (var blogAttachment in blogAttachments)
        {
          await _blogAttachmentService.UpdateBlogIdAsync(blogAttachment.BlogAttachmentID, blog.BlogID, GetOrganizationId()!.Value);
          await _blogAttachmentService.MoveAndUpdateBlogAttachmentPathAsync(
              blogAttachment,
              ConfigurationSingleton.Instance.PermPath,
              GetOrganizationId()!.Value,
              GetDatabaseName()
          );

          //// combine perm path with file name
          //string fullPath = Path.Combine(
          //    ConfigurationSingleton.Instance.PermPath!,
          //    GetDatabaseName(),
          //    Path.GetFileName(blogAttachment.FilePath)
          //);

          //bool validMp4 = await _ffmpegService.CheckAsync(fullPath);

          //if (validMp4)
          //{
          //  await _ffmpegService.ScheduleEncoderAsync(fullPath);
          //}
        }

        scope.Complete();
      }

      return RedirectToAction("Blogs");
    }

    [HttpGet]
    [Route("blogs")]
    public IActionResult Blogs(
      int page = 1,
      int pageSize = 2)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;

      var vm = new BlogsPaginatedViewModel
      {
        Page = page,
        PageSize = pageSize,
        RememberPageSize = string.IsNullOrEmpty(referer),
      };

      return View(vm);
    }

    [HttpGet]
    [Route("public-blogs")]
    [AllowAnonymous]
    public IActionResult PublicBlogs(
      int page = 1,
      int pageSize = 2)
    {
      var referer = Request.Headers["Referer"].ToString() ?? string.Empty;
      var vm = new BlogsPaginatedViewModel
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
  [Route("api/blog")]
  public class BlogApiController : BaseController
  {
    private readonly BlogService _blogService;

    public BlogApiController(RequestContext requestContext, BlogService blogService)
    {
      _blogService = new BlogService(
        requestContext.DatabaseName,
        requestContext.DatabasePassword);
    }

    [HttpGet("get-public-blogs")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicBlogs(
      int page = 1,
      int pageSize = 2)
    {
      var (blogs, nextPage) = await _blogService.GetAllPublicAsync(page, pageSize);

      var markdownPipeline = new Markdig.MarkdownPipelineBuilder()
          .Build();

      var sanitizer = new HtmlSanitizer();

      GetBlogsViewModel getBlogsViewModel = new GetBlogsViewModel
      {
        Blogs = blogs.Select(b => new GetBlogsViewModel.BlogViewModel
        {
          BlogID = b.BlogID,
          PublicId = b.PublicId,
          Title = b.Title,
          Content = sanitizer.Sanitize(Markdig.Markdown.ToHtml(b.Content, markdownPipeline)),
          RowNumber = b.RowNumber
        }).ToList(),
        Page = page,
        NextPage = nextPage,
      };

      return Ok(getBlogsViewModel);
    }

    [HttpGet("get-blogs")]
    public async Task<IActionResult> GetBlogs(
      int page = 1,
      int pageSize = 2)
    {
      var (blogs, nextPage) = await _blogService.GetAllAsync(page, pageSize);

      var markdownPipeline = new Markdig.MarkdownPipelineBuilder()
          .Build();

      var sanitizer = new HtmlSanitizer();
      GetBlogsViewModel getBlogsViewModel = new GetBlogsViewModel
      {
        Blogs = blogs.Select(b => new GetBlogsViewModel.BlogViewModel
        {
          BlogID = b.BlogID,
          PublicId = b.PublicId,
          Title = b.Title,
          Content = sanitizer.Sanitize(Markdig.Markdown.ToHtml(b.Content, markdownPipeline)),
          RowNumber = b.RowNumber
        }).ToList(),
        Page = page,
        NextPage = nextPage,
      };

      return Ok(getBlogsViewModel);
    }
  }
}