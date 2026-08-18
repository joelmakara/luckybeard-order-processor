using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RefactoringExercise.Data;
using RefactoringExercise.Email;
using RefactoringExercise.Models;
using RefactoringExercise.Payments;

namespace RefactoringExercise.Services;

public class OrderProcessor(
    OrdersDbContext db,
    IEnumerable<IPaymentStrategy> paymentStrategies,
    IEmailSender emailSender,
    ILogger<OrderProcessor> logger) : IOrderProcessor
{
    private const string ConfirmationEmailSubject = "Order Confirmation";

    public async Task<OrderResult> ProcessOrderAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        OrderResult? result = null;
        PaymentResult? payment = null;

        try
        {
            (result, payment) = await ProcessAsync(request, cancellationToken);
            return result;
        }
        finally
        {
            logger.LogInformation(
                "Order attempt: customer {CustomerId}, {ItemCount} item(s), method {PaymentMethod}, " +
                "payment {PaymentOutcome}/{ProviderStatusCode}, outcome {Outcome}, total {Total}, {ElapsedMs:0} ms",
                request.CustomerId,
                request.Items.Count,
                request.PaymentMethod,
                payment?.Outcome,
                payment?.ProviderStatusCode,
                result?.Outcome.ToString() ?? "UnhandledException",
                result?.Total,
                Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }

    private async Task<(OrderResult Result, PaymentResult? Payment)> ProcessAsync(
        ProcessRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, out var paymentMethod))
        {
            return (new OrderResult(OrderOutcome.InvalidPaymentMethod, 0), null);
        }

        var strategy = paymentStrategies.FirstOrDefault(s => s.Method == paymentMethod);
        if (strategy is null)
        {
            return (new OrderResult(OrderOutcome.InvalidPaymentMethod, 0), null);
        }

        var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            return (new OrderResult(OrderOutcome.CustomerNotFound, 0), null);
        }

        var products = await db.Products
            .Where(p => request.Items.Contains(p.Name))
            .ToListAsync(cancellationToken);

        decimal total = 0;
        foreach (var item in request.Items)
        {
            var product = products.FirstOrDefault(p => p.Name == item);
            if (product is null)
            {
                return (new OrderResult(OrderOutcome.ProductNotFound, 0), null);
            }

            total += product.Price;
        }

        if (request.DiscountPercentage > 0)
        {
            total -= total * (request.DiscountPercentage / 100m);
        }

        total = Math.Round(total, 2, MidpointRounding.AwayFromZero);

        var payment = await strategy.ChargeAsync(total, cancellationToken);
        if (payment.Outcome == PaymentOutcome.Declined)
        {
            return (new OrderResult(OrderOutcome.PaymentFailed, total), payment);
        }

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            db.Orders.Add(new Order
            {
                CustomerId = request.CustomerId,
                Total = total,
                Status = payment.Outcome == PaymentOutcome.Pending ? OrderStatus.Pending : OrderStatus.Completed,
                PaymentMethod = paymentMethod
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        try
        {
            await emailSender.SendAsync(
                request.CustomerEmail,
                ConfirmationEmailSubject,
                $"Your order has been placed. Total: ${total}",
                cancellationToken);

            return (new OrderResult(OrderOutcome.Completed, total), payment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Confirmation email to {Email} failed for customer {CustomerId}",
                request.CustomerEmail, request.CustomerId);
            return (new OrderResult(OrderOutcome.CompletedEmailFailed, total), payment);
        }
    }

    public Task<List<Order>> GetOrderHistoryAsync(int customerId, CancellationToken cancellationToken = default)
        => db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
}
