# Technical Assessment: .NET Refactoring & Architecture Challenge

This repository holds my solution to Lucky Beard's .NET refactoring assessment.
The brief below is copied verbatim from the assessment page (an external
Confluence share), since shared links can expire. The starter code it refers to
is preserved unmodified in the baseline commit.

---

## Introduction to the file

You have inherited a legacy Order Processor Flow that has become unwieldy and hard to maintain.

Identify issues, improve upon the file.

This is not a real flow, so do not worry too much about actual flows running end-to-end.

Please add mock endpoints to the flow.

Make use of .net 8-10 concepts.

## Candidate Tasks

```csharp
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace RefactoringExercise
{
    // This is the messy code that needs refactoring.

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class ProcessRequest
    {
        public int CustomerId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Discount { get; set; }
    }

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
}
```

## 1. Overview

You have inherited a legacy Order Processor Flow that has become unwieldy and hard to maintain.

Your task is to review, analyse, and refactor the component into a solution that is:

- Scalable
- Maintainable
- Performance
- Well-structured

The goal is not to shorten the code, but to improve its architecture, separation of concerns, and long-term maintainability.

We are interested in how the candidate thinks about:

- Architectural problems
- Code security
- .NET standards and Best practices
- Single Responsibility and good Locality of Behavior Thinking
- Interface segregation
- Decoupling
- Developer experience and maintainability thinking.

## 2. What We Would Like to See

What are some specific things we want to see in the solution.

- Strategy Pattern [bonus]
- Dependency Injection
- Validation of Data
- Logging (preferably wide)
- Good Error Handling (coupled with good logging)
- DB Transactions
- No hard-coded Secrets, magic strings or magic numbers

## 3. Key Known Issues

- SQL Injection Vulnerability
- Hard-coded DB Credentials
- This code is a god-class that handles everything in the whole flow
- Bad/No exception handling
- Method/Variable naming
- Memory leaks (not disposing of connections)
- No dependency injection/tight coupling
