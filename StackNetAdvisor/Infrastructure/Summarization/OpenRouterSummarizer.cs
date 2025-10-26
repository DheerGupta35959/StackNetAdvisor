using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using StackNetAdvisor.Core.Contracts;

namespace StackNetAdvisor.Infrastructure.Summarization;

public class OpenRouterSummarizer : ISummarizer
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string? _apiKey;
    private readonly string _referer;
    private readonly string _title;

    public OpenRouterSummarizer(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _model = configuration["OpenRouter:Model"] ?? "openrouter/auto";
        _apiKey = configuration["OpenRouter:ApiKey"];
        _referer = configuration["OpenRouter:Site"] ?? "https://localhost";
        _title = configuration["OpenRouter:Title"] ?? "StackNetAdvisor";
    }

    public async Task<string> SummarizeAsync(string question, IEnumerable<string> answerBodies, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OpenRouter:ApiKey not configured in appsettings.json");

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        if (!_http.DefaultRequestHeaders.Contains("HTTP-Referer"))
            _http.DefaultRequestHeaders.Add("HTTP-Referer", _referer);
        if (!_http.DefaultRequestHeaders.Contains("X-Title"))
            _http.DefaultRequestHeaders.Add("X-Title", _title);

        var prompt = BuildPrompt(question, answerBodies);

        var payload = new ChatRequest
        {
            Model = _model,
            Messages = new object[]
            {
                new { role = "system", content = "You are a helpful .NET assistant. Summarize answers into concise, actionable bullets." },
                new { role = "user", content = prompt }
            },
            Temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);
        using var resp = await _http.PostAsync("https://openrouter.ai/api/v1/chat/completions", new StringContent(json, Encoding.UTF8, "application/json"), ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"OpenRouter summarization failed: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }

        var respJson = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(respJson);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? "No summary available.";
    }

    private static string BuildPrompt(string question, IEnumerable<string> answers)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Question: {question}");
        sb.AppendLine("Summarize the best practices and key takeaways:");
        int i = 1;
        foreach (var a in answers)
        {
            var plain = System.Text.RegularExpressions.Regex.Replace(a, "<[^>]+>", string.Empty);
            sb.AppendLine($"Answer {i++}:\n{plain}\n");
        }
        sb.AppendLine("Return 3–6 bullets with code identifiers in backticks where helpful, and prefer .NET 8 guidance.");
        return sb.ToString();
    }

    private class ChatRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("messages")] public object[] Messages { get; set; } = Array.Empty<object>();
        [JsonPropertyName("temperature")] public double Temperature { get; set; }
    }
}