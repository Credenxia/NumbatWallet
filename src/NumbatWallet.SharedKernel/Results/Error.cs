namespace NumbatWallet.SharedKernel.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public static Error NotFound(string entityName, object id) =>
        new($"Error.NotFound", $"{entityName} with id '{id}' was not found.");

    public static Error Validation(string code, string message) =>
        new($"Error.Validation.{code}", message);

    public static Error BusinessRule(string rule, string message) =>
        new($"Error.BusinessRule.{rule}", message);

    public static Error Unauthorized(string message = "Unauthorized access.") =>
        new("Error.Unauthorized", message);

    public static Error Forbidden(string message = "Access forbidden.") =>
        new("Error.Forbidden", message);

    public static Error Conflict(string message) =>
        new("Error.Conflict", message);

    public static implicit operator Result(Error error) => Result.Failure(error);
}