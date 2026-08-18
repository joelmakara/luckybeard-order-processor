using RefactoringExercise.Models;

namespace RefactoringExercise.Payments;

public interface IPaymentStrategy
{
    PaymentMethod Method { get; }

    Task<PaymentResult> ChargeAsync(decimal amount, CancellationToken cancellationToken = default);
}
