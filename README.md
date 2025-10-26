# StackNetAdvisor

A .NET 8 console Q&A bot that analyzes Stack Overflow posts and provides summarized .NET advice.

## Features

- Accepts natural language .NET questions
- Retrieves relevant Stack Overflow threads via Stack Exchange API
- Summarizes top answers via OpenRouter (or local heuristic fallback)
- Shows a concise summary and top thread link
- Clean architecture with DI:
  - `StackNetAdvisor.Core` – models, contracts, AdvisorService orchestrator
  - `StackNetAdvisor.Infrastructure` – Stack Exchange client, caching, summarizers
  - `StackNetAdvisor.ConsoleApp` – CLI entrypoint and composition root
- Optional local caching to avoid redundant API calls

## Prerequisites

- .NET SDK 8.0+
- Internet access for Stack Exchange API (optional OpenRouter access)

## Configuration (appsettings.json)

This app now uses `appsettings.json` for configuration.

- Location: `StackNetAdvisor/ConsoleApp/appsettings.json`
- Example:
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
- Keys:
  - `OpenRouter:ApiKey` enables AI summarization via OpenRouter
  - `OpenRouter:Model` selects the model (defaults to `openrouter/auto`)
  - `OpenRouter:Site` sets the Referer header value (optional)
  - `OpenRouter:Title` sets an app title header (optional)
  - `StackExchange:Key` optionally improves reliability/quota for Stack Exchange
  - `Cache:Directory` sets local cache folder name

Note: Environment variables can still be used as an override if desired, but the default is to read from `appsettings.json`.

## Setup

1. Restore and build:
   - `dotnet build StackNetAdvisor.sln`

2. Update `appsettings.json` with your keys (see above).

3. Run the console app:
   - `dotnet run --project StackNetAdvisor/ConsoleApp`

You can also pipe a question for quick testing:

- `echo "How can I make async file I/O faster in C#?" | dotnet run --project StackNetAdvisor/ConsoleApp`

## Example

```
Ask your .NET question:
> How can I make async file I/O faster in C#?

Searching Stack Overflow...

🔍 Found 5 relevant posts
✅ Summary:
- Use async streams or `FileStream` with `useAsync: true`
- Avoid blocking calls inside async methods
- Use `ValueTask` for lightweight async operations

📙 Top Thread: `https://stackoverflow.com/questions/1234567`
```

## Architecture Notes

- `AdvisorService` orchestrates retrieval, summarization, and caching.
- `StackOverflowClient` hits `search/advanced` with `.net` and `c#` tags and falls back to `search` endpoint if needed.
- Summarization uses `OpenRouterSummarizer` if `OpenRouter:ApiKey` is configured; otherwise `SimpleSummarizer` provides a local heuristic summary.
- `JsonFileCacheProvider` stores cached results in `ConsoleApp/bin/<configuration>/cache` by default.

## Error Handling

- Network/API failures surface friendly messages (e.g., 403/timeout). The app continues with fallback behaviors where possible.
- If summarization fails, the app falls back to a heuristic summary.

## Notes

- For consistent API access, consider creating a Stack Exchange app key and setting `StackExchange:Key` in appsettings.
- OpenRouter is OpenAI API compatible; set `OpenRouter:ApiKey` to switch on AI summarization.

## Branding

- Maintained by `Dheer Gupta`
- Website: `https://dheergupta.in`
- This build includes branding for Dheer Gupta and `dheergupta.in`.