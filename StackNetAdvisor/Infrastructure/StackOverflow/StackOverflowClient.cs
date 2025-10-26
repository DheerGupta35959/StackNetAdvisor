using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StackNetAdvisor.Core.Contracts;
using StackNetAdvisor.Core.Models;

namespace StackNetAdvisor.Infrastructure.StackOverflow;

public class StackOverflowClient : IStackOverflowClient
{
    private readonly HttpClient _http;
    private readonly string? _stackExKey;

    public StackOverflowClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.stackexchange.com/2.3/");
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("StackNetAdvisor/1.0 (+https://example.local)");
        _stackExKey = configuration["StackExchange:Key"];
    }

    public async Task<IReadOnlyList<StackPost>> SearchPostsAsync(string query, int limit = 5, CancellationToken ct = default)
    {
        var q = Uri.EscapeDataString(query);
        var keyParam = string.IsNullOrWhiteSpace(_stackExKey) ? string.Empty : $"&key={_stackExKey}";
        var url = $"search/advanced?order=desc&sort=relevance&q={q}&tagged=.net;c%23&site=stackoverflow&pagesize={limit}{keyParam}";
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            // Fallback to simpler search endpoint
            var fallbackUrl = $"search?order=desc&sort=relevance&intitle={q}&site=stackoverflow&pagesize={limit}{keyParam}";
            using var resp2 = await _http.GetAsync(fallbackUrl, ct);
            if (!resp2.IsSuccessStatusCode)
                throw new HttpRequestException($"StackOverflow search failed: {(int)resp2.StatusCode} {resp2.ReasonPhrase}");
            var json2 = await resp2.Content.ReadAsStringAsync(ct);
            return ParseQuestions(json2, limit);
        }
        var json = await resp.Content.ReadAsStringAsync(ct);
        return ParseQuestions(json, limit);
    }

    private IReadOnlyList<StackPost> ParseQuestions(string json, int limit)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var parsed = JsonSerializer.Deserialize<ApiResponse<QuestionDto>>(json, options) ?? new ApiResponse<QuestionDto>();
        var posts = parsed.Items.Select(i => new StackPost
        {
            QuestionId = i.QuestionId,
            Title = i.Title ?? string.Empty,
            Link = i.Link ?? string.Empty,
            Score = i.Score,
            AcceptedAnswerId = i.AcceptedAnswerId
        }).OrderByDescending(p => p.Score).Take(limit).ToList();
        return posts;
    }

    public async Task<IReadOnlyList<Answer>> GetTopAnswersAsync(int questionId, int limit = 1, CancellationToken ct = default)
    {
        var url = $"questions/{questionId}/answers?order=desc&sort=votes&site=stackoverflow&filter=withbody&pagesize={limit}";
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"StackOverflow answers failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }
        var json = await resp.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var parsed = JsonSerializer.Deserialize<ApiResponse<AnswerDto>>(json, options) ?? new ApiResponse<AnswerDto>();
        var answers = parsed.Items.Select(a => new Answer
        {
            AnswerId = a.AnswerId,
            Body = a.Body ?? string.Empty,
            Score = a.Score,
            IsAccepted = a.IsAccepted
        }).OrderByDescending(a => a.Score).Take(limit).ToList();
        return answers;
    }

    private class ApiResponse<T>
    {
        public List<T> Items { get; set; } = new();
    }
    private class QuestionDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("question_id")]
        public int QuestionId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string? Title { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("link")]
        public string? Link { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("score")]
        public int Score { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("accepted_answer_id")]
        public int? AcceptedAnswerId { get; set; }
    }
    private class AnswerDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("answer_id")]
        public int AnswerId { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("body")]
        public string? Body { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("score")]
        public int Score { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("is_accepted")]
        public bool IsAccepted { get; set; }
    }
}