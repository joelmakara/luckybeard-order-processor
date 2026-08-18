using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RefactoringExercise.Models;
using RefactoringExercise.Options;

namespace RefactoringExercise.Services;

public class OrderProcessor(
    IConfiguration configuration,
    IOptions<SmtpOptions> smtpOptions,
    IOptions<PaymentProviderOptions> paymentProviderOptions) : IOrderProcessor
{
    private readonly string _connectionString = configuration.GetConnectionString("Orders")
        ?? throw new InvalidOperationException("Connection string 'Orders' is not configured.");

    public OrderResult ProcessOrder(ProcessRequest request)
    {
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, out var paymentMethod))
        {
            return new OrderResult(OrderOutcome.InvalidPaymentMethod, 0);
        }

        var conn = new SqlConnection(_connectionString);
        conn.Open();

        decimal total = 0;
        foreach (var item in request.Items)
        {
            var cmd = new SqlCommand("SELECT Price FROM Products WHERE Name = '" + item + "'", conn);
            var price = (decimal)cmd.ExecuteScalar();
            total += price;
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

        if (paymentResult.Contains("success") || paymentResult == "pending")
        {
            var insertCmd = new SqlCommand(
                "INSERT INTO Orders (CustomerId, Total, Status, PaymentMethod) VALUES (" +
                request.CustomerId + ", " + total + ", 'Completed', '" + paymentMethod + "')", conn);
            insertCmd.ExecuteNonQuery();

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

                conn.Close();
                return new OrderResult(OrderOutcome.Completed, total);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the order
                Console.WriteLine("Email failed: " + ex.Message);
                conn.Close();
                return new OrderResult(OrderOutcome.CompletedEmailFailed, total);
            }
        }
        else
        {
            conn.Close();
            return new OrderResult(OrderOutcome.PaymentFailed, total);
        }
    }

    public List<string> FindHistory(string customerId)
    {
        var conn = new SqlConnection(_connectionString);
        conn.Open();

        var cmd = new SqlCommand("SELECT * FROM Orders WHERE CustomerId = " + customerId, conn);
        var reader = cmd.ExecuteReader();

        List<string> orders = [];
        while (reader.Read())
        {
            orders.Add("Order #" + reader["Id"] + " - $" + reader["Total"] + " - " + reader["Status"]);
        }

        conn.Close();
        return orders;
    }
}
