using System.Collections.Generic;

namespace StackNetAdvisor.Core.Models;

public class AdvisorResult
{
    public string Summary { get; set; } = string.Empty;
    public List<StackPost> TopPosts { get; set; } = new();
}