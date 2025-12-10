using Application.Common.Models;
using FluentValidation;
using MediatR;

namespace Application.Common.Behaviours
{
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults
                    .Where(r => r.Errors.Any())
                    .SelectMany(r => r.Errors)
                    .ToList();

                if (failures.Any())
                {
                    var firstFailure = failures.First();
                    Error error;

                    if (firstFailure.CustomState is Error customError)
                    {
                        error = customError;
                    }
                    else
                    {
                        // TODO: Review error code for generic validation errors
                        error = new Error(400, "Validation.Error", firstFailure.ErrorMessage);
                    }

                    var responseType = typeof(TResponse);

                    if (responseType == typeof(Response))
                    {
                        return (TResponse)(object)Response.Failure(error);
                    }

                    if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Response<>))
                    {
                        var resultType = responseType.GetGenericArguments()[0];
                        
                        var failureMethod = typeof(Response)
                            .GetMethods()
                            .First(m => m.Name == "Failure" && m.IsGenericMethod && m.GetParameters().Length == 1)
                            .MakeGenericMethod(resultType);

                        return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
                    }

                    throw new ValidationException(failures);
                }
            }
            return await next();
        }
    }
}
