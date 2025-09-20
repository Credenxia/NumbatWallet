namespace NumbatWallet.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException() : base()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class BusinessRuleException : Exception
{
    public BusinessRuleException() : base()
    {
    }

    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class ValidationException : Exception
{
    public Dictionary<string, List<string>> Errors { get; }

    public ValidationException() : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, List<string>>();
    }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, List<string>>();
    }

    public ValidationException(Dictionary<string, List<string>> errors) : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}