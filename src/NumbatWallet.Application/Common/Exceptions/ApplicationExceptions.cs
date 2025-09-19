namespace NumbatWallet.Application.Common.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityType, string id)
        : base($"{entityType} with ID '{id}' was not found.")
    {
        EntityType = entityType;
        EntityId = id;
    }

    public string EntityType { get; }
    public string EntityId { get; }
}

public class UnauthorizedIssuerException : Exception
{
    public UnauthorizedIssuerException(string issuerId)
        : base($"Issuer with ID '{issuerId}' is not authorized or is inactive.")
    {
        IssuerId = issuerId;
    }

    public string IssuerId { get; }
}

public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }

    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message)
    {
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}