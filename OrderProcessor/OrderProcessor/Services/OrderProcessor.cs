using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;

namespace RefactoringExercise.Services;

public class OrderProcessor
{
    public string ProcessOrder(int customerId, string customerEmail, List<string> items, string paymentMethod, decimal discount)
    {
        var conn = new SqlConnection("Server=localhost;Database=Orders;User Id=sa;Password=P@ssw0rd;");
        conn.Open();

        decimal total = 0;
        foreach (var item in items)
        {
            var cmd = new SqlCommand("SELECT Price FROM Products WHERE Name = '" + item + "'", conn);
            var price = (decimal)cmd.ExecuteScalar();
            total += price;
        }

        if (discount > 0)
        {
            total -= total * discount;
        }

        if (paymentMethod != "CreditCard" && paymentMethod != "PayPal" && paymentMethod != "BankTransfer")
        {
            conn.Close();
            return "Invalid payment method";
        }

        // Payment processing response codes
        // 202 = success
        // 409 = duplicate order
        // 500 = server error
        // 400 = bad request
        string paymentResult = string.Empty;
        if (paymentMethod == "CreditCard")
        {
            var client = new WebClient();
            paymentResult = client.DownloadString("https://api.payment.com/charge?amount=" + total);
        }
        else if (paymentMethod == "PayPal")
        {
            var client = new WebClient();
            paymentResult = client.DownloadString("https://api.paypal.com/charge?amount=" + total);
        }
        else if (paymentMethod == "BankTransfer")
        {
            paymentResult = "pending";
        }

        if (paymentResult.Contains("success") || paymentResult == "pending")
        {
            var insertCmd = new SqlCommand(
                "INSERT INTO Orders (CustomerId, Total, Status, PaymentMethod) VALUES (" +
                customerId + ", " + total + ", 'Completed', '" + paymentMethod + "')", conn);
            insertCmd.ExecuteNonQuery();

            try
            {
                var smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("noreply@company.com", "P@ssw0rd123");
                smtp.EnableSsl = true;

                var mail = new MailMessage();
                mail.From = new MailAddress("noreply@company.com");
                mail.To.Add(customerEmail);
                mail.Subject = "Order Confirmation";
                mail.Body = "Your order has been placed. Total: $" + total;

                smtp.Send(mail);

                conn.Close();
                return "Order processed successfully. Total: $" + total;
            }
            catch (Exception ex)
            {
                // Log error but don't fail the order
                Console.WriteLine("Email failed: " + ex.Message);
                conn.Close();
                return "Order processed but email failed. Total: $" + total;
            }
        }
        else
        {
            conn.Close();
            return "Payment failed";
        }
    }

    public List<string> FindHistory(string customerId)
    {
        var conn = new SqlConnection("Server=localhost;Database=Orders;User Id=sa;Password=P@ssw0rd;");
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
