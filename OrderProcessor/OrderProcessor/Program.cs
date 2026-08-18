using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RefactoringExercise.Options;
using RefactoringExercise.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<PaymentProviderOptions>(
    builder.Configuration.GetSection(PaymentProviderOptions.SectionName));

builder.Services.AddScoped<IOrderProcessor, OrderProcessor>();

using var host = builder.Build();
