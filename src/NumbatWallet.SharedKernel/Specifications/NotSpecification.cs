using System.Linq.Expressions;

namespace NumbatWallet.SharedKernel.Specifications;

/// <summary>
/// Negates a specification
/// </summary>
public class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expression = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var visitor = new ReplaceExpressionVisitor(expression.Parameters[0], parameter);
        var body = Expression.Not(visitor.Visit(expression.Body)!);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}