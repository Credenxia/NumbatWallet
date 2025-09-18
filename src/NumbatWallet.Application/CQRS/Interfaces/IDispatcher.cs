namespace NumbatWallet.Application.CQRS.Interfaces;

/// <summary>
/// Dispatcher for CQRS commands and queries
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Sends a command that returns a result
    /// </summary>
    /// <typeparam name="TResult">The type of result</typeparam>
    /// <param name="command">The command to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The command result</returns>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command that doesn't return a result
    /// </summary>
    /// <param name="command">The command to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns a result
    /// </summary>
    /// <typeparam name="TResult">The type of result</typeparam>
    /// <param name="query">The query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The query result</returns>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}