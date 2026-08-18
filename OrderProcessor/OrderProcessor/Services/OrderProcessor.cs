using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RefactoringExercise.Data;
using RefactoringExercise.Models;
using RefactoringExercise.Options;

namespace RefactoringExercise.Services;

public class OrderProcessor(
    OrdersDbContext db,
    IOptions<SmtpOptions> smtpOptions,
    IOptions<PaymentProviderOptions> paymentProviderOptions) : IOrderProcessor
{
    public async Task<OrderResult> ProcessOrderAsync(ProcessRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, out var paymentMethod))
        {
            return new OrderResult(OrderOutcome.InvalidPaymentMethod, 0);
        }

        var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            return new OrderResult(OrderOutcome.CustomerNotFound, 0);
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
                return new OrderResult(OrderOutcome.ProductNotFound, 0);
            }

            total += product.Price;
        }

        if (request.Discount > 0)
        {
            total -= total * request.Discount;
        }

        var providers = paymentProviderOptions.Value;
        string paymentResult = string.Empty;
        if (paymentMethod == PaymentMethod.CreditCard)
        {
            var client = new WebClient();
            paymentResult = client.DownloadString(providers.CreditCardChargeUrl + "?amount=" + total);
        }
        else if (paymentMethod == PaymentMethod.PayPal)
        {
            var client = new WebClient();
            paymentResult = client.DownloadString(providers.PayPalChargeUrl + "?amount=" + total);
        }
        else if (paymentMethod == PaymentMethod.BankTransfer)
        {
            paymentResult = "pending";
        }

        if (!paymentResult.Contains("success") && paymentResult != "pending")
        {
            return new OrderResult(OrderOutcome.PaymentFailed, total);
        }

        // The transaction deliberately covers only database work; it must
        // never be held open across the external payment or email calls.
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            db.Orders.Add(new Order
            {
                CustomerId = request.CustomerId,
                Total = total,
                Status = paymentResult == "pending" ? OrderStatus.Pending : OrderStatus.Completed,
                PaymentMethod = paymentMethod
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        var smtpSettings = smtpOptions.Value;
        try
        {
            var smtp = new SmtpClient(smtpSettings.Host, smtpSettings.Port);
            smtp.Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password);
            smtp.EnableSsl = true;

            var mail = new MailMessage();
            mail.From = new MailAddress(smtpSettings.FromAddress);
            mail.To.Add(request.CustomerEmail);
            mail.Subject = "Order Confirmation";
            mail.Body = "Your order has been placed. Total: $" + total;

            smtp.Send(mail);

            return new OrderResult(OrderOutcome.Completed, total);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the order
            Console.WriteLine("Email failed: " + ex.Message);
            return new OrderResult(OrderOutcome.CompletedEmailFailed, total);
        }
    }

    public Task<List<Order>> GetOrderHistoryAsync(int customerId, CancellationToken cancellationToken = default)
        => db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
}
