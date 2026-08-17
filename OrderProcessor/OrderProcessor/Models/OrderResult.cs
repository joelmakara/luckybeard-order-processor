namespace RefactoringExercise.Models;

public record OrderResult(OrderOutcome Outcome, decimal Total)
{
    public bool IsSuccess => Outcome is OrderOutcome.Completed or OrderOutcome.CompletedEmailFailed;
}
