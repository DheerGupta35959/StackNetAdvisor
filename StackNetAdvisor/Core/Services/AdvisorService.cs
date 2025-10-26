using StackNetAdvisor.Core.Contracts;
using StackNetAdvisor.Core.Models;

namespace StackNetAdvisor.Core.Services;

public class AdvisorService
{
    private readonly IStackOverflowClient _soClient;
    private readonly ISummarizer _summarizer;
    private readonly ICacheProvider? _cache;

    public AdvisorService(IStackOverflowClient soClient, ISummarizer summarizer, ICacheProvider? cache = null)
    {
        _soClient = soClient;
        _summarizer = summarizer;
        _cache = cache;
    }

    public async Task<AdvisorResult> AskAsync(string question, CancellationToken ct = default)
    {
        var cacheKey = $"qa:{question.Trim().ToLowerInvariant()}";
        if (_cache is not null)
        {
            var cached = await _cache.GetAsync<AdvisorResult>(cacheKey, ct);
            if (cached is not null) return cached;
        }

        var posts = await _soClient.SearchPostsAsync(question, limit: 5, ct);
        var answerBodies = new List<string>();

        foreach (var post in posts.Take(3))
        {
            var answers = await _soClient.GetTopAnswersAsync(post.QuestionId, limit: 1, ct);
            var body = answers.FirstOrDefault()?.Body;
            if (!string.IsNullOrWhiteSpace(body))
                answerBodies.Add(body);
        }

        string summary;
        try
        {
            summary = await _summarizer.SummarizeAsync(question, answerBodies, ct);
        }
        catch
        {
            // Fallback to a naive summary if summarizer fails
            summary = BuildSimpleSummary(answerBodies);
        }

        var result = new AdvisorResult
        {
            Summary = summary,
            TopPosts = posts.ToList()
        };

        if (_cache is not null)
        {
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(12), ct);
        }

        return result;
    }

    private static string BuildSimpleSummary(IEnumerable<string> answerBodies)
    {
        var bullets = new List<string>();
        foreach (var body in answerBodies)
        {
            var plain = HtmlToText(body);
            foreach (var line in plain.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                // Heuristics for useful lines
                if (trimmed.StartsWith("-") || trimmed.Contains("async") || trimmed.Contains("LINQ") || trimmed.Contains("FileStream"))
                {
                    bullets.Add(trimmed.TrimStart('-','*',' '));
                }
            }
        }
        if (bullets.Count == 0) bullets.Add("Review top answers; avoid blocking; prefer async APIs.");
        return string.Join("\n- ", new[] { "" }.Concat(bullets));
    }

    private static string HtmlToText(string html)
    {
        // Minimal HTML stripper; good enough for console summaries
        var withoutTags = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(withoutTags);
    }
}