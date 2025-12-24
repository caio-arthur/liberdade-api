using API.ExceptionHandlers;
using API.Workers;
using Application;
using Application.Common.DTOs;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Auth Configuration
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LiberdadeDbContext>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

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

var googleCredentialPath = builder.Configuration["GoogleCloud:GoogleCredentialPath"];

if (string.IsNullOrEmpty(googleCredentialPath) || !File.Exists(googleCredentialPath))
{
    throw new FileNotFoundException(
            $"CRÍTICO: O arquivo de credenciais do Google não foi encontrado. " +
            $"Verifique se a configuração 'GoogleCloud' aponta para um arquivo válido. " +
            $"Caminho tentado: '{googleCredentialPath}'");
}

builder.Services.AddSingleton(sp =>
{
    var credential = CredentialFactory.FromFile<ServiceAccountCredential>(googleCredentialPath)
                                  .ToGoogleCredential();
    return TranslationClient.Create(credential);
});

builder.Services.AddHostedService<AtualizarMercadoWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("AllowAll");

// Migrate and Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<LiberdadeDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var config = services.GetRequiredService<IConfiguration>();

        context.Database.Migrate();
        await SeedData.SeedAsync(context, userManager, roleManager, config);
    }
    catch (Exception ex)
    {
        throw new Exception("Ocorreu um erro no processo de migração ou seeding do database.", ex);
    }
}

app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

