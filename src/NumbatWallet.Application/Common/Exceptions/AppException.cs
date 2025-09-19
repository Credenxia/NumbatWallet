namespace NumbatWallet.Application.Common.Exceptions;

/// <summary>
/// Base exception for application layer errors
/// </summary>
public class AppException : Exception
{
    public string Code { get; }

    public AppException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public AppException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}