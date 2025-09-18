namespace NumbatWallet.SharedKernel.Models;

/// <summary>
/// Validation result model
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; private set; } = true;
    public List<ValidationError> Errors { get; } = new();

    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    public static ValidationResult Failure(params ValidationError[] errors)
    {
        var result = new ValidationResult { IsValid = false };
        result.Errors.AddRange(errors);
        return result;
    }

    public void AddError(string field, string message, string? code = null)
    {
        IsValid = false;
        Errors.Add(new ValidationError(field, message, code));
    }

    public void AddErrors(IEnumerable<ValidationError> errors)
    {
        IsValid = false;
        Errors.AddRange(errors);
    }
}

public record ValidationError(string Field, string Message, string? Code = null);