using StackNetAdvisor.Core.Models;

namespace StackNetAdvisor.Core.Contracts;

public interface IStackOverflowClient
{
    Task<IReadOnlyList<StackPost>> SearchPostsAsync(string query, int limit = 5, CancellationToken ct = default);
    Task<IReadOnlyList<Answer>> GetTopAnswersAsync(int questionId, int limit = 1, CancellationToken ct = default);
}