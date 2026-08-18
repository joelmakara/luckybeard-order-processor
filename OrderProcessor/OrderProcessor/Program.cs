using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RefactoringExercise.Data;
using RefactoringExercise.Options;
using RefactoringExercise.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<PaymentProviderOptions>(
    builder.Configuration.GetSection(PaymentProviderOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("Connection string 'Orders' is not configured.");

builder.Services.AddDbContext<OrdersDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IOrderProcessor, OrderProcessor>();

using var host = builder.Build();
