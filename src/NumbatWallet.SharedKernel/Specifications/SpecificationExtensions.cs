using System.Linq.Expressions;

namespace NumbatWallet.SharedKernel.Specifications;

public static class SpecificationExtensions
{
    /// <summary>
    /// Converts an ISpecification to its underlying Expression for use with LINQ queries.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="specification">The specification to convert</param>
    /// <returns>The expression representing the specification criteria</returns>
    public static Expression<Func<T, bool>> ToExpression<T>(this ISpecification<T> specification)
    {
        return specification.Criteria;
    }
}