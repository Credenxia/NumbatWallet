using System.Linq.Expressions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Application.Specifications;

public class CredentialByTenantSpecification : BaseSpecification<Credential>
{
    public CredentialByTenantSpecification(Guid tenantId)
        : base(c => c.TenantId == tenantId.ToString())
    {
    }
}

public class CredentialByWalletSpecification : BaseSpecification<Credential>
{
    public CredentialByWalletSpecification(Guid walletId, Guid tenantId)
        : base(c => c.WalletId == walletId && c.TenantId == tenantId.ToString())
    {
    }
}

public class ActiveCredentialByWalletSpecification : BaseSpecification<Credential>
{
    public ActiveCredentialByWalletSpecification(Guid walletId, Guid tenantId)
        : base(c => c.WalletId == walletId &&
                    c.TenantId == tenantId.ToString() &&
                    c.Status != CredentialStatus.Revoked)
    {
    }
}

public class CredentialSearchSpecification : BaseSpecification<Credential>
{
    public CredentialSearchSpecification(
        Guid tenantId,
        string? searchTerm = null,
        string? credentialType = null,
        CredentialStatus? status = null)
        : base(BuildExpression(tenantId, searchTerm, credentialType, status))
    {
    }

    private static Expression<Func<Credential, bool>> BuildExpression(
        Guid tenantId,
        string? searchTerm,
        string? credentialType,
        CredentialStatus? status)
    {
        Expression<Func<Credential, bool>> expression = c => c.TenantId == tenantId.ToString();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            expression = CombineExpressions(expression, c =>
                c.CredentialType.Contains(searchTerm) ||
                c.IssuerId.ToString().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(credentialType))
        {
            expression = CombineExpressions(expression, c => c.CredentialType == credentialType);
        }

        if (status.HasValue)
        {
            expression = CombineExpressions(expression, c => c.Status == status.Value);
        }

        return expression;
    }

    private static Expression<Func<T, bool>> CombineExpressions<T>(
        Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var rightBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);

        var combined = Expression.AndAlso(leftBody, rightBody);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}