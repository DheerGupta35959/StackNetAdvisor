namespace StackNetAdvisor.Core.Models;

public class StackPost
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public int Score { get; set; }
    public int? AcceptedAnswerId { get; set; }
}