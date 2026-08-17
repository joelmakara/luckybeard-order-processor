using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;

namespace RefactoringExercise.Services;

public class OrderProcessor
{
    public string DoProcess(int id, string c_email, List<string> i, string method, double disc)
    {
        // Connect to database
        var conn = new SqlConnection("Server=localhost;Database=Orders;User Id=sa;Password=P@ssw0rd;");
        conn.Open();

        // Calculate total
        double total = 0;
        foreach (var item in i)
        {
            var cmd = new SqlCommand("SELECT Price FROM Products WHERE Name = '" + item + "'", conn);
            var price = (double)cmd.ExecuteScalar();
            total = total + price;
        }

        // Apply discount
        if (disc > 0)
        {
            total = total - (total * disc);
        }

        // Validate payment method
        if (method != "CreditCard" && method != "PayPal" && method != "BankTransfer")
        {
            conn.Close();
            return "Invalid payment method";
        }

        // Process payment
        // Payment processing response codes
        // 202 = success
        // 409 = duplicate order
        // 500 = server error
        // 400 = bad request
        string pAyMeNtReSuLt = "";
        if (method == "CreditCard")
        {
            // Call credit card API
            var client = new WebClient();
            pAyMeNtReSuLt = client.DownloadString("https://api.payment.com/charge?amount=" + total);
        }
        else if (method == "PayPal")
        {
            // Call PayPal API
            var client = new WebClient();
            pAyMeNtReSuLt = client.DownloadString("https://api.paypal.com/charge?amount=" + total);
        }
        else if (method == "BankTransfer")
        {
            // Bank transfer doesn't need API call
            pAyMeNtReSuLt = "pending";
        }

        if (pAyMeNtReSuLt.Contains("success") || pAyMeNtReSuLt == "pending")
        {
            // Save order to database
            var insertCmd = new SqlCommand(
                "INSERT INTO Orders (CustomerId, Total, Status, PaymentMethod) VALUES (" +
                id + ", " + total + ", 'Completed', '" + method + "')", conn);
            insertCmd.ExecuteNonQuery();

            // Send confirmation email
            try
            {
                var smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("noreply@company.com", "P@ssw0rd123");
                smtp.EnableSsl = true;

                var mail = new MailMessage();
                mail.From = new MailAddress("noreply@company.com");
                mail.To.Add(c_email);
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


    public List<string> FindHistory(string customer_id)
    {
        var conn = new SqlConnection("Server=localhost;Database=Orders;User Id=sa;Password=P@ssw0rd;");
        conn.Open();

        var cmd = new SqlCommand("SELECT * FROM Orders WHERE CustomerId = " + customer_id, conn);
        var rEdAdEr = cmd.ExecuteReader();

        var orders = new List<string>();
        while (rEdAdEr.Read())
        {
            orders.Add("Order #" + rEdAdEr["Id"] + " - $" + rEdAdEr["Total"] + " - " + rEdAdEr["Status"]);
        }

        conn.Close();
        return orders;
    }
}
