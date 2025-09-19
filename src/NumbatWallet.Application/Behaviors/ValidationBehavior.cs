using FluentValidation;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Behaviors;

/// <summary>
/// Validation behavior for CQRS commands and queries
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : ICommandHandler<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly ICommandHandler<TRequest, TResponse> _innerHandler;
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        ICommandHandler<TRequest, TResponse> innerHandler,
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _innerHandler = innerHandler;
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (_validators.Any())
        {
            _logger.LogDebug("Validating {RequestType}", typeof(TRequest).Name);

            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(failure => failure != null)
                .ToList();

            if (failures.Count != 0)
            {
                _logger.LogWarning("Validation failed for {RequestType} with {ErrorCount} errors",
                    typeof(TRequest).Name, failures.Count);

                var errors = failures
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                throw new ValidationException(errors);
            }

            _logger.LogDebug("Validation passed for {RequestType}", typeof(TRequest).Name);
        }

        return await _innerHandler.HandleAsync(request, cancellationToken);
    }
}

/// <summary>
/// Custom validation exception that includes property-specific errors
/// </summary>
public class ValidationException : Exception
{
    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
