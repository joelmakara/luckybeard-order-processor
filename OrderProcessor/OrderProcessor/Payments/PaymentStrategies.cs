using System.Globalization;
using Microsoft.Extensions.Options;
using RefactoringExercise.Models;
using RefactoringExercise.Options;

namespace RefactoringExercise.Payments;

public abstract class HttpChargePaymentStrategy(HttpClient httpClient) : IPaymentStrategy
{
    public abstract PaymentMethod Method { get; }

    protected abstract string ChargeUrl { get; }

    public async Task<PaymentResult> ChargeAsync(decimal amount, CancellationToken cancellationToken = default)
    {
        var url = $"{ChargeUrl}?amount={amount.ToString(CultureInfo.InvariantCulture)}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        return PaymentResult.FromStatusCode((int)response.StatusCode);
    }
}

public class CreditCardPaymentStrategy(HttpClient httpClient, IOptions<PaymentProviderOptions> options)
    : HttpChargePaymentStrategy(httpClient)
{
    public override PaymentMethod Method => PaymentMethod.CreditCard;

    protected override string ChargeUrl => options.Value.CreditCardChargeUrl;
}

public class PayPalPaymentStrategy(HttpClient httpClient, IOptions<PaymentProviderOptions> options)
    : HttpChargePaymentStrategy(httpClient)
{
    public override PaymentMethod Method => PaymentMethod.PayPal;

    protected override string ChargeUrl => options.Value.PayPalChargeUrl;
}

public class BankTransferPaymentStrategy : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.BankTransfer;

    public Task<PaymentResult> ChargeAsync(decimal amount, CancellationToken cancellationToken = default)
        => Task.FromResult(PaymentResult.Pending());
}
