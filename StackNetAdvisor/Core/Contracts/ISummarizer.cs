namespace StackNetAdvisor.Core.Contracts;

public interface ISummarizer
{
    Task<string> SummarizeAsync(string question, IEnumerable<string> answerBodies, CancellationToken ct = default);
}