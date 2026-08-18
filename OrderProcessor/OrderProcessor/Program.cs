using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpLogging;
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

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
    ?? new DatabaseOptions();

builder.Services.AddDbContext<OrdersDbContext>(options =>
{
    if (databaseOptions.Provider == DatabaseProvider.Sqlite)
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

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

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpLogging(options =>
    options.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<OrdersDbContext>());
}

app.MapControllers();

app.Run();
