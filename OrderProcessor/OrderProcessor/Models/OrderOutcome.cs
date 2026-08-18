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
