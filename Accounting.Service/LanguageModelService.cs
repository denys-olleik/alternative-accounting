using Accounting.Business;
using RestSharp;
using System.Text.Json;

namespace Accounting.Service
{
  public class LanguageModelService
  {
    public async Task<(string response, T? structuredResponse)> GenerateResponse<T>(
      string prompt,
      string? context,
      bool useMiniModel,
      bool requireStructuredResponse = false)
    {
      SecretService secretService = new SecretService();
      Secret secret = await secretService.GetAsync(Secret.SecretTypeConstants.OpenAI, 1);

      string model = useMiniModel ? "gpt-4.1-mini-2025-04-14" : "gpt-4.1-2025-04-14";

      var client = new RestClient("https://api.openai.com");
      var request = new RestRequest("/v1/chat/completions", Method.POST);
      request.AddHeader("Authorization", $"Bearer {secret.Value}");
      request.AddHeader("Content-Type", "application/json");

      var requestBody = new
      {
        model = model,
        messages = new[]
        {
          new { role = "user", content = prompt }
        },
        temperature = 0.1
      };
      request.AddJsonBody(requestBody);

      var response = await client.ExecuteAsync(request);

      if (!response.IsSuccessful)
      {
        throw new InvalidOperationException($"API call failed: {response.StatusCode} - {response.ErrorMessage}");
      }

      using var jsonDoc = JsonDocument.Parse(response.Content!);
      string responseContent = jsonDoc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString() ?? string.Empty;

      if (requireStructuredResponse)
      {
        try
        {
          T structuredResponse = JsonSerializer.Deserialize<T>(responseContent);
          return (context, structuredResponse);
        }
        catch
        {
          throw new InvalidOperationException("Failed to parse structured response.");
        }
      }

      return ($"{context}\n{responseContent}", default(T));
    }
  }
}