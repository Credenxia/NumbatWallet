namespace NumbatWallet.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key {key} was not found.") { }
    public NotFoundException(string message) : base(message) { }
}

public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string businessRule)
        : base($"Business rule violation: {businessRule}") { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message)
        : base(message) { }
}