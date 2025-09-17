namespace NumbatWallet.SharedKernel.Exceptions;

public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string rule, string message)
        : base($"BUSINESS_RULE_{rule}", message)
    {
        Rule = rule;
    }

    public string Rule { get; }
}