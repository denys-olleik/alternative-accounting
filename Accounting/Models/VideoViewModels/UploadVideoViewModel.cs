namespace Accounting.Models.VideoViewModels
{
  public class UploadVideoViewModel
  {
    public string Title { get; set; }
    public IFormFile File { get; set; }
    public string Description { get; set; }
  }
}