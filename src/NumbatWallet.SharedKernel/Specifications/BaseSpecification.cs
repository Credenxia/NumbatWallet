using System.Linq.Expressions;

namespace NumbatWallet.SharedKernel.Specifications;

public abstract class BaseSpecification<T> : ISpecification<T>
{
    protected BaseSpecification()
    {
        Includes = new List<Expression<Func<T, object>>>();
        IncludeStrings = new List<string>();
    }

    protected BaseSpecification(Expression<Func<T, bool>> criteria) : this()
    {
        Criteria = criteria;
    }

    public Expression<Func<T, bool>> Criteria { get; private set; } = null!;
    public List<Expression<Func<T, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public Expression<Func<T, object>>? GroupBy { get; private set; }
    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    protected void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
    }
}