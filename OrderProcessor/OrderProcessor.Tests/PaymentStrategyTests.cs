using System.Net;
using Microsoft.Extensions.Options;
using RefactoringExercise.Options;
using RefactoringExercise.Payments;

namespace RefactoringExercise.Tests;

public class PaymentStrategyTests
{
    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private static readonly IOptions<PaymentProviderOptions> ProviderOptions =
        Microsoft.Extensions.Options.Options.Create(new PaymentProviderOptions
        {
            CreditCardChargeUrl = "http://provider.test/charge",
            PayPalChargeUrl = "http://paypal.test/charge"
        });

    [Fact]
    public async Task Credit_card_approves_on_202_and_formats_amount_invariantly()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.Accepted);
        var strategy = new CreditCardPaymentStrategy(new HttpClient(handler), ProviderOptions);

        var result = await strategy.ChargeAsync(12.5m);

        Assert.Equal(PaymentOutcome.Approved, result.Outcome);
        Assert.Equal(PaymentStatusCode.Success, result.ProviderStatusCode);
        Assert.Equal("http://provider.test/charge?amount=12.5", handler.LastRequestUri!.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, PaymentStatusCode.ServerError)]
    [InlineData(HttpStatusCode.Conflict, PaymentStatusCode.DuplicateOrder)]
    [InlineData(HttpStatusCode.BadRequest, PaymentStatusCode.BadRequest)]
    public async Task Provider_failures_decline_with_the_provider_code(HttpStatusCode httpStatus, PaymentStatusCode expected)
    {
        var handler = new StubHttpMessageHandler(httpStatus);
        var strategy = new PayPalPaymentStrategy(new HttpClient(handler), ProviderOptions);

        var result = await strategy.ChargeAsync(100m);

        Assert.Equal(PaymentOutcome.Declined, result.Outcome);
        Assert.Equal(expected, result.ProviderStatusCode);
    }

    [Fact]
    public async Task Bank_transfer_is_pending_without_a_provider_call()
    {
        var result = await new BankTransferPaymentStrategy().ChargeAsync(100m);

        Assert.Equal(PaymentOutcome.Pending, result.Outcome);
        Assert.Null(result.ProviderStatusCode);
    }
}
