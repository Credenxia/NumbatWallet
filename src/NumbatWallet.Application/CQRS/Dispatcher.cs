using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.CQRS;

/// <summary>
/// Default implementation of the CQRS dispatcher
/// </summary>
public class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Dispatcher> _logger;

    public Dispatcher(IServiceProvider serviceProvider, ILogger<Dispatcher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));

        _logger.LogDebug("Dispatching command {CommandType}", commandType.Name);

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            _logger.LogError("No handler registered for command {CommandType}", commandType.Name);
            throw new InvalidOperationException($"No handler registered for command {commandType.Name}");
        }

        try
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                throw new InvalidOperationException($"HandleAsync method not found on handler for {commandType.Name}");
            }

            var task = handleMethod.Invoke(handler, new object[] { command, cancellationToken }) as Task<TResult>;
            if (task == null)
            {
                throw new InvalidOperationException($"HandleAsync did not return a Task<{typeof(TResult).Name}>");
            }

            var result = await task;

            _logger.LogDebug("Command {CommandType} handled successfully", commandType.Name);
            return result;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error handling command {CommandType}", commandType.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

        _logger.LogDebug("Dispatching command {CommandType}", commandType.Name);

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            _logger.LogError("No handler registered for command {CommandType}", commandType.Name);
            throw new InvalidOperationException($"No handler registered for command {commandType.Name}");
        }

        try
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                throw new InvalidOperationException($"HandleAsync method not found on handler for {commandType.Name}");
            }

            var task = handleMethod.Invoke(handler, new object[] { command, cancellationToken }) as Task;
            if (task == null)
            {
                throw new InvalidOperationException("HandleAsync did not return a Task");
            }

            await task;

            _logger.LogDebug("Command {CommandType} handled successfully", commandType.Name);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error handling command {CommandType}", commandType.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));

        _logger.LogDebug("Dispatching query {QueryType}", queryType.Name);

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            _logger.LogError("No handler registered for query {QueryType}", queryType.Name);
            throw new InvalidOperationException($"No handler registered for query {queryType.Name}");
        }

        try
        {
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                throw new InvalidOperationException($"HandleAsync method not found on handler for {queryType.Name}");
            }

            var task = handleMethod.Invoke(handler, new object[] { query, cancellationToken }) as Task<TResult>;
            if (task == null)
            {
                throw new InvalidOperationException($"HandleAsync did not return a Task<{typeof(TResult).Name}>");
            }

            var result = await task;

            _logger.LogDebug("Query {QueryType} handled successfully", queryType.Name);
            return result;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error handling query {QueryType}", queryType.Name);
            throw;
        }
    }
}