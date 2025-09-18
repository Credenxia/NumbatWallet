namespace NumbatWallet.SharedKernel.Results;

public sealed record DomainError(string Code, string Message)
{
    public static readonly DomainError None = new(string.Empty, string.Empty);
    public static readonly DomainError NullValue = new("Error.NullValue", "A null value was provided.");

    public static DomainError NotFound(string entityName, object id) =>
        new($"Error.NotFound", $"{entityName} with id '{id}' was not found.");

    public static DomainError Validation(string code, string message) =>
        new($"Error.Validation.{code}", message);

    public static DomainError BusinessRule(string rule, string message) =>
        new($"Error.BusinessRule.{rule}", message);

    public static DomainError Unauthorized(string message = "Unauthorized access.") =>
        new("Error.Unauthorized", message);

    public static DomainError Forbidden(string message = "Access forbidden.") =>
        new("Error.Forbidden", message);

    public static DomainError Conflict(string message) =>
        new("Error.Conflict", message);
}