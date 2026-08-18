namespace RefactoringExercise.Models;

public enum OrderStatus
{
    Pending,
    Completed
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}
