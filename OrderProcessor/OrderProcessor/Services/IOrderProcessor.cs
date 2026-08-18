using RefactoringExercise.Models;

namespace RefactoringExercise.Services;

public interface IOrderProcessor
{
    Task<OrderResult> ProcessOrderAsync(ProcessRequest request, CancellationToken cancellationToken = default);

    Task<List<Order>> GetOrderHistoryAsync(int customerId, CancellationToken cancellationToken = default);
}
