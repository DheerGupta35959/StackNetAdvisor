# StackNetAdvisor: Turning Stack Overflow into Actionable .NET Advice

StackNetAdvisor is a lightweight .NET 8 console bot that searches Stack Overflow for your .NET questions and distills top answers into a concise, practical summary. Built with a clean architecture and optional AI summarization via OpenRouter, it’s a fast way to get signal over noise when exploring solutions.

## Why This Exists
- Faster discovery: Get the essence of multiple answers without reading long threads.
- Actionable guidance: Focus on key practices, pitfalls, and code identifiers.
- Works offline for AI: Falls back to a heuristic summarizer if AI is not configured.

## Highlights
- .NET 8 console app with DI and clear boundaries
- Stack Exchange API client with graceful fallbacks
- OpenRouter AI summarizer or local `SimpleSummarizer`
- Optional JSON file cache to avoid redundant requests

## Architecture Overview
- `StackNetAdvisor.Core` — Contracts, models, and `AdvisorService` orchestration
- `StackNetAdvisor.Infrastructure` — StackOverflow client, caching, summarizers
- `StackNetAdvisor.ConsoleApp` — Composition root, configuration, and CLI

Flow:
1. Read your question.
2. Search Stack Overflow and pick relevant posts.
3. Fetch top-voted answers.
4. Summarize (OpenRouter if enabled, otherwise local heuristics).
5. Print bullets and the top thread link.

## Configuration via appsettings.json
Update `StackNetAdvisor/ConsoleApp/appsettings.json`:

```json
{
  "OpenRouter": {
    "ApiKey": "sk-or-xxxxxxxxxxxxxxxxxxxxxxxx",
    "Model": "openrouter/auto",
    "Site": "https://yourdomain.example",
    "Title": "StackNetAdvisor"
  },
  "StackExchange": {
    "Key": "your-stack-exchange-app-key"
  },
  "Cache": {
    "Directory": "cache"
  }
}
```

- `OpenRouter:ApiKey` turns on AI summaries (OpenRouter)
- `OpenRouter:Model` selects the summarization model
- `StackExchange:Key` improves reliability/quota with Stack Exchange

## Get Started
- Build: `dotnet build StackNetAdvisor.sln`
- Run: `dotnet run --project StackNetAdvisor/ConsoleApp`
- Example:

```
Ask your .NET question:
> How can I make async file I/O faster in C#?

🔍 Found 5 relevant posts
✅ Summary:
- Use async streams or `FileStream` with `useAsync: true`
- Avoid blocking calls inside async methods
- Prefer buffered writes and avoid tiny chunks

📙 Top Thread: https://stackoverflow.com/questions/1234567
```

## OpenRouter Summaries
When `OpenRouter:ApiKey` is set, summaries are generated via the OpenRouter API using a compact prompt that strips HTML from answers and asks for 3–6 bullet points with inline code identifiers. If OpenRouter is not configured or an error occurs, the app falls back to `SimpleSummarizer`.

## Error Handling
- Fallback search: If advanced search fails, we try a simpler endpoint.
- Friendly messages: Network/API issues surface concise guidance.
- Caching: Optional local JSON cache to reduce duplicate calls.

## Roadmap
- Rich CLI flags (pagesize, tags, output format)
- Markdown/HTML output
- SQLite cache provider
- Per-model prompt tuning

## Credits & Branding
- Maintained by Dheer Gupta — `https://dheergupta.in`
- Stack Overflow data via Stack Exchange API
- AI summaries via OpenRouter (optional)

If you’d like a web UI or GitHub Actions automation to batch summarize threads, I’m happy to extend this. Open an issue or reach out via the site above.