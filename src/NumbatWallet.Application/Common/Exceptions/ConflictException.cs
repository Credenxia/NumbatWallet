namespace NumbatWallet.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public string? Code { get; }

    public ConflictException()
    {
    }

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string code, string message) : base(message)
    {
        Code = code;
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}