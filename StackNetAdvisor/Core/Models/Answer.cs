namespace StackNetAdvisor.Core.Models;

public class Answer
{
    public int AnswerId { get; set; }
    public string Body { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsAccepted { get; set; }
}