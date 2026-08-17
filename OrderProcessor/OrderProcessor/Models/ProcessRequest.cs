namespace RefactoringExercise.Models;

public class ProcessRequest
{
    public int CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public List<string> Items { get; set; } = [];
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Discount { get; set; }
}
