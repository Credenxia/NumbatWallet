namespace NumbatWallet.SharedKernel.Exceptions;

public class ValidationException : DomainException
{
    public ValidationException(string message)
        : base("VALIDATION_ERROR", message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}