using Microsoft.EntityFrameworkCore;
using RefactoringExercise.Controllers;
using RefactoringExercise.Data;
using RefactoringExercise.Email;
using RefactoringExercise.Options;
using RefactoringExercise.Payments;
using RefactoringExercise.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<PaymentProviderOptions>(
    builder.Configuration.GetSection(PaymentProviderOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("Connection string 'Orders' is not configured.");

builder.Services.AddDbContext<OrdersDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHttpClient<CreditCardPaymentStrategy>();
builder.Services.AddHttpClient<PayPalPaymentStrategy>();
builder.Services.AddScoped<BankTransferPaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategy>(sp => sp.GetRequiredService<CreditCardPaymentStrategy>());
builder.Services.AddScoped<IPaymentStrategy>(sp => sp.GetRequiredService<PayPalPaymentStrategy>());
builder.Services.AddScoped<IPaymentStrategy>(sp => sp.GetRequiredService<BankTransferPaymentStrategy>());

if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, NoOpEmailSender>();
}

builder.Services.AddScoped<IOrderProcessor, OrderProcessor>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
