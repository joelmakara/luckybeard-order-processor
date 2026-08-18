namespace RefactoringExercise.Options;

public class PaymentProviderOptions
{
    public const string SectionName = "PaymentProviders";

    public string CreditCardChargeUrl { get; set; } = string.Empty;
    public string PayPalChargeUrl { get; set; } = string.Empty;
}
