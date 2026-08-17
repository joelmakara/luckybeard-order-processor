namespace RefactoringExercise.Models;

/// <summary>
/// Response codes returned by the external payment providers.
/// </summary>
public enum PaymentStatusCode
{
    Success = 202,
    BadRequest = 400,
    DuplicateOrder = 409,
    ServerError = 500
}
