using Application.Common.Exceptions;
using Application.Common.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace API.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, 
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Ocorreu uma exce��o: {Message}", exception.Message);

            var statusCode = (int)HttpStatusCode.InternalServerError;
            var errorResponse = new Error(500, "Server.Error", "Ocorreu um erro interno.");

            if (exception is DomainException domainEx)
            {
                statusCode = domainEx.Error.Codigo;
                errorResponse = domainEx.Error;
            }
            else if (exception is ValidationException validationEx)
            {
                statusCode = validationEx.Error.Codigo;
                errorResponse = validationEx.Error;
            }

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = statusCode;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(Response.Failure(errorResponse), jsonOptions);

            await httpContext.Response.WriteAsync(json, cancellationToken);

            return true;
        }
    }
}