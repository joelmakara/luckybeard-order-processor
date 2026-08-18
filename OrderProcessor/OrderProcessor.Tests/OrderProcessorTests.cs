using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RefactoringExercise.Data;
using RefactoringExercise.Email;
using RefactoringExercise.Models;
using RefactoringExercise.Payments;
using RefactoringExercise.Services;

namespace RefactoringExercise.Tests;

public class OrderProcessorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OrdersDbContext _db;

    public OrderProcessorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseSqlite(_connection).Options;
        _db = new OrdersDbContext(options);
        _db.Database.EnsureCreated();

        _db.Customers.Add(new Customer { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" });
        _db.Products.AddRange(
            new Product { Name = "Keyboard", Price = 60m },
            new Product { Name = "Mouse", Price = 40m });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private sealed class FakePaymentStrategy(PaymentMethod method, PaymentResult result) : IPaymentStrategy
    {
        public decimal? ChargedAmount { get; private set; }

        public PaymentMethod Method => method;

        public Task<PaymentResult> ChargeAsync(decimal amount, CancellationToken cancellationToken = default)
        {
            ChargedAmount = amount;
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public bool Sent { get; private set; }

        public Exception? ToThrow { get; init; }

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (ToThrow is not null)
            {
                throw ToThrow;
            }

            Sent = true;
            return Task.CompletedTask;
        }
    }

    private OrderProcessor CreateProcessor(IPaymentStrategy strategy, IEmailSender? emailSender = null)
        => new(_db, [strategy], emailSender ?? new RecordingEmailSender(), NullLogger<OrderProcessor>.Instance);

    private static ProcessRequest Request(
        string method = "CreditCard", decimal discountPercentage = 0, List<string>? items = null, int customerId = 1)
        => new()
        {
            CustomerId = customerId,
            CustomerEmail = "ada@example.com",
            Items = items ?? ["Keyboard", "Mouse"],
            PaymentMethod = method,
            DiscountPercentage = discountPercentage
        };

    [Fact]
    public async Task Approved_payment_completes_and_persists_the_order()
    {
        var strategy = new FakePaymentStrategy(PaymentMethod.CreditCard, PaymentResult.FromStatusCode(202));
        var email = new RecordingEmailSender();
        var processor = CreateProcessor(strategy, email);

        var result = await processor.ProcessOrderAsync(Request());

        Assert.Equal(OrderOutcome.Completed, result.Outcome);
        Assert.Equal(100m, result.Total);
        Assert.True(result.IsSuccess);
        Assert.True(email.Sent);

        var order = Assert.Single(_db.Orders);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(PaymentMethod.CreditCard, order.PaymentMethod);
        Assert.Equal(100m, order.Total);
    }

    [Fact]
    public async Task Discount_percentage_is_applied_to_the_charged_amount()
    {
        var strategy = new FakePaymentStrategy(PaymentMethod.CreditCard, PaymentResult.FromStatusCode(202));
        var processor = CreateProcessor(strategy);

        var result = await processor.ProcessOrderAsync(Request(discountPercentage: 10));

        Assert.Equal(90m, result.Total);
        Assert.Equal(90m, strategy.ChargedAmount);
    }

    [Fact]
    public async Task Bank_transfer_is_persisted_as_pending()
    {
        var strategy = new FakePaymentStrategy(PaymentMethod.BankTransfer, PaymentResult.Pending());
        var processor = CreateProcessor(strategy);

        var result = await processor.ProcessOrderAsync(Request(method: "BankTransfer"));

        Assert.Equal(OrderOutcome.Completed, result.Outcome);
        var order = Assert.Single(_db.Orders);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public async Task Declined_payment_fails_and_persists_nothing()
    {
        var strategy = new FakePaymentStrategy(PaymentMethod.CreditCard, PaymentResult.FromStatusCode(500));
        var processor = CreateProcessor(strategy);

        var result = await processor.ProcessOrderAsync(Request());

        Assert.Equal(OrderOutcome.PaymentFailed, result.Outcome);
        Assert.False(result.IsSuccess);
        Assert.Empty(_db.Orders);
    }

    [Fact]
    public async Task Email_failure_keeps_the_order()
    {
        var strategy = new FakePaymentStrategy(PaymentMethod.CreditCard, PaymentResult.FromStatusCode(202));
        var email = new RecordingEmailSender { ToThrow = new InvalidOperationException("smtp down") };
        var processor = CreateProcessor(strategy, email);

        var result = await processor.ProcessOrderAsync(Request());

        Assert.Equal(OrderOutcome.CompletedEmailFailed, result.Outcome);
        Assert.True(result.IsSuccess);
        Assert.Single(_db.Orders);
    }

    [Fact]
    public async Task Unknown_customer_is_rejected_before_charging()
    {
        var strategy = new FakePaymentStrategy(PaymentMethod.CreditCard, PaymentResult.FromStatusCode(202));
        var processor = CreateProcessor(strategy);

        var result = await processor.ProcessOrderAsync(Request(customerId: 999));

        Assert.Equal(OrderOutcome.CustomerNotFound, result.Outcome);
        Assert.Null(strategy.ChargedAmount);
        Assert.Empty(_db.Orders);
    }

    [Fact]
    public async Task Unknown_product_is_rejected_before_charging()
    {
        var strategy = new FakePaymentStrategy(PaymentMethod.CreditCard, PaymentResult.FromStatusCode(202));
        var processor = CreateProcessor(strategy);

        var result = await processor.ProcessOrderAsync(Request(items: ["Gramophone"]));

        Assert.Equal(OrderOutcome.ProductNotFound, result.Outcome);
        Assert.Null(strategy.ChargedAmount);
        Assert.Empty(_db.Orders);
    }

    [Fact]
    public async Task Unknown_payment_method_is_rejected()
    {
        var strategy = new FakePaymentStrategy(PaymentMethod.CreditCard, PaymentResult.FromStatusCode(202));
        var processor = CreateProcessor(strategy);

        var result = await processor.ProcessOrderAsync(Request(method: "Cheque"));

        Assert.Equal(OrderOutcome.InvalidPaymentMethod, result.Outcome);
        Assert.Empty(_db.Orders);
    }

    [Fact]
    public async Task History_returns_only_the_requested_customers_orders_newest_first()
    {
        _db.Customers.Add(new Customer { Id = 2, Name = "Grace Hopper", Email = "grace@example.com" });
        _db.Orders.AddRange(
            new Order { CustomerId = 1, Total = 10m, Status = OrderStatus.Completed, PaymentMethod = PaymentMethod.CreditCard },
            new Order { CustomerId = 2, Total = 20m, Status = OrderStatus.Completed, PaymentMethod = PaymentMethod.PayPal },
            new Order { CustomerId = 1, Total = 30m, Status = OrderStatus.Pending, PaymentMethod = PaymentMethod.BankTransfer });
        _db.SaveChanges();

        var processor = CreateProcessor(new FakePaymentStrategy(PaymentMethod.CreditCard, PaymentResult.Pending()));

        var history = await processor.GetOrderHistoryAsync(1);

        Assert.Equal(2, history.Count);
        Assert.All(history, o => Assert.Equal(1, o.CustomerId));
        Assert.Equal(30m, history[0].Total);
    }
}
