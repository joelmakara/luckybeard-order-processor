namespace RefactoringExercise.Models;

public enum OrderOutcome
{
    Completed,
    CompletedEmailFailed,
    PaymentFailed,
    InvalidPaymentMethod,
    CustomerNotFound,
    ProductNotFound
}

public record OrderResult(OrderOutcome Outcome, decimal Total)
{
    public bool IsSuccess => Outcome is OrderOutcome.Completed or OrderOutcome.CompletedEmailFailed;
}
