using RefactoringExercise.Models;

namespace RefactoringExercise.Services;

public interface IOrderProcessor
{
    OrderResult ProcessOrder(ProcessRequest request);

    List<string> FindHistory(string customerId);
}
