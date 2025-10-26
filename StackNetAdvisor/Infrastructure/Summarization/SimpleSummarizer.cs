using StackNetAdvisor.Core.Contracts;

namespace StackNetAdvisor.Infrastructure.Summarization;

public class SimpleSummarizer : ISummarizer
{
    public Task<string> SummarizeAsync(string question, IEnumerable<string> answerBodies, CancellationToken ct = default)
    {
        // Very basic heuristic summarizer (no external APIs)
        var bullets = new List<string>
        {
            "Prefer async APIs; avoid blocking calls in async methods",
            "Use efficient LINQ; consider `AsSpan`, `Select` vs `SelectMany` wisely",
            "For file I/O, use `FileStream` with `useAsync: true`",
        };

        // Add one or two lines derived from answers (strip HTML)
        foreach (var body in answerBodies.Take(2))
        {
            var text = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]+>", string.Empty);
            var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).Take(2);
            bullets.AddRange(lines.Select(l => l.Length > 120 ? l[..120] + "…" : l));
        }

        var result = string.Join("\n- ", new[] { "" }.Concat(bullets));
        return Task.FromResult(result);
    }
}