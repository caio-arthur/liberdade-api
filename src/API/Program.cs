using API.ExceptionHandlers;
using API.Workers;
using Application;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<DiarioFinanceiroWorker>();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddHttpClient("StatusInvest", client =>
{
    client.BaseAddress = new Uri("https://statusinvest.com.br/");
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
});

builder.Services.AddHttpClient("BCB", client =>
{
    client.BaseAddress = new Uri("https://api.bcb.gov.br/");
    client.DefaultRequestHeaders.Add("User-Agent", "LiberdadeApi/1.0");
});

builder.Services.AddHostedService<AtualizarMercadoWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Migrate and Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<LiberdadeDbContext>();
        context.Database.Migrate();
        SeedData.Seed(context);
    }
    catch (Exception ex)
    {
        throw new Exception("Ocorreu um erro no processo de migração ou seeding do database.", ex);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); 
app.MapControllers();

app.Run();
