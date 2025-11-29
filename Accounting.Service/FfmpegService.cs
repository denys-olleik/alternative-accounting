
namespace Accounting.Service
{
  public class FfmpegService : BaseService
  {
    public FfmpegService() : base()
    {

    }

    public FfmpegService(
      string databaseName,
      string databasePassword)
    {

    }

    // Check to make sure mp4 file is actually an mp4 file.
    public async Task<bool> CheckAsync(string filePath)
    {
      var ffprobePath = "ffprobe";
      var args = $"-v error -show_entries format=format_name -show_streams -of json \"{filePath}\"";

      var processStartInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = ffprobePath,
        Arguments = args,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using (var process = System.Diagnostics.Process.Start(processStartInfo))
      {
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        var json = System.Text.Json.JsonDocument.Parse(output);

        if (!json.RootElement.TryGetProperty("format", out var formatElem))
          return false;

        if (!formatElem.TryGetProperty("format_name", out var formatNameElem))
          return false;

        var formatName = formatNameElem.GetString();
        if (string.IsNullOrEmpty(formatName) || !formatName.Contains("mp4"))
          return false;

        if (!json.RootElement.TryGetProperty("streams", out var streamsElem))
          return false;

        foreach (var stream in streamsElem.EnumerateArray())
        {
          if (stream.TryGetProperty("codec_type", out var codecTypeElem) &&
              codecTypeElem.GetString() == "video")
          {
            return true;
          }
        }

        return false;
      }
    }

    public async Task ScheduleEncoderAsync(string fullPath)
    {
      throw new NotImplementedException();
    }
  }
}