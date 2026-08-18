namespace RefactoringExercise.Payments;

public enum PaymentOutcome
{
    Approved,
    Pending,
    Declined
}

public enum PaymentStatusCode
{
    Success = 202,
    BadRequest = 400,
    DuplicateOrder = 409,
    ServerError = 500
}

public record PaymentResult(PaymentOutcome Outcome, PaymentStatusCode? ProviderStatusCode = null)
{
    public static PaymentResult Pending() => new(PaymentOutcome.Pending);

    public static PaymentResult FromStatusCode(int statusCode)
    {
        var code = (PaymentStatusCode)statusCode;
        return code == PaymentStatusCode.Success
            ? new PaymentResult(PaymentOutcome.Approved, code)
            : new PaymentResult(PaymentOutcome.Declined, code);
    }
}
