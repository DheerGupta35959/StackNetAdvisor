using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackNetAdvisor.Core.Contracts;
using StackNetAdvisor.Core.Services;
using StackNetAdvisor.Infrastructure.Caching;
using StackNetAdvisor.Infrastructure.StackOverflow;
using StackNetAdvisor.Infrastructure.Summarization;

var services = new ServiceCollection();

// Configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();
services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(b => b.AddConsole());

// StackOverflow client (reads StackExchange key from configuration)
services.AddHttpClient<IStackOverflowClient, StackOverflowClient>();

// Summarizer: prefer OpenRouter when key is present, else SimpleSummarizer
var openRouterKey = configuration["OpenRouter:ApiKey"];
if (!string.IsNullOrWhiteSpace(openRouterKey))
{
    Console.WriteLine("OpenRouter summarizer enabled via appsettings");
    services.AddHttpClient<OpenRouterSummarizer>();
    services.AddTransient<ISummarizer>(sp => sp.GetRequiredService<OpenRouterSummarizer>());
}
else
{
    services.AddSingleton<ISummarizer, SimpleSummarizer>();
}

// Optional cache
services.AddSingleton<ICacheProvider>(sp => new JsonFileCacheProvider());

// Advisor orchestrator
services.AddTransient<AdvisorService>();

var provider = services.BuildServiceProvider();
var advisor = provider.GetRequiredService<AdvisorService>();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("StackNetAdvisor");

Console.WriteLine("Ask your .NET question:");
Console.Write("> ");
var question = Console.ReadLine();
if (string.IsNullOrWhiteSpace(question))
{
    Console.WriteLine("No question provided. Please try again.");
    return;
}

Console.WriteLine();
Console.WriteLine("Searching Stack Overflow...");

try
{
    var result = await advisor.AskAsync(question!);
    Console.WriteLine();
    Console.WriteLine("\uD83D\uDD0D Found {0} relevant posts", result.TopPosts.Count);
    Console.WriteLine("\u2705 Summary:");
    Console.WriteLine(result.Summary.StartsWith("- ") ? result.Summary : "- " + result.Summary);
    Console.WriteLine();
    if (result.TopPosts.Count > 0)
    {
        var top = result.TopPosts[0];
        Console.WriteLine("\uD83D\uDCD9 Top Thread: `{0}`", top.Link);
    }
    else
    {
        Console.WriteLine("No threads found. Try refining your question.");
    }
}
catch (HttpRequestException ex)
{
    // Likely API/network error
    logger.LogError(ex, "API request failed");
    Console.WriteLine("Sorry, I couldn't reach Stack Overflow right now. Please check your internet connection or try again later.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error");
    Console.WriteLine("An unexpected error occurred. Please try again. Details: {0}", ex.Message);
}
