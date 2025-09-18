namespace NumbatWallet.Application.CQRS.Interfaces;

/// <summary>
/// Marker interface for commands that don't return a result
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marker interface for commands that return a result
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command</typeparam>
public interface ICommand<TResult>
{
}