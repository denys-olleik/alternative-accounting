using Accounting.Business;

namespace Accounting.Service
{
  public class LanguageModelService
  {
    public async Task<(string context, T? structuredResponse)> GenerateResponse<T>(
      string prompt,
      string context,
      bool useMiniModel,
      bool requireStructuredResponse = false)
    {
      SecretService secretService = new SecretService();
      Secret secret = await secretService.GetAsync(Secret.SecretTypeConstants.OpenAI, 1);

      // call OpenAI API with the provided prompt and secret
      // if response is structured, parse it into T
      // otherwise put response into context and leave structured response as null
      using (HttpClient client = new HttpClient())
      {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secret.Value);
        var requestBody = new
        {
          model = "gpt-4.1-2025-04-14",
          messages = new[]
          {
            new { role = "user", content = prompt }
          },
          temperature = 0.1
        };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json"
        );
        HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();
        string jsonResponse = await response.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
        string responseContent = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        if (requireStructuredResponse)
        {
          try
          {
            T structuredResponse = System.Text.Json.JsonSerializer.Deserialize<T>(responseContent);
            return (context, structuredResponse);
          }
          catch
          {
            throw new InvalidOperationException("Failed to parse structured response.");
          }
        }

        return (context + "\n" + responseContent, default(T));
      }
    }
  }
}