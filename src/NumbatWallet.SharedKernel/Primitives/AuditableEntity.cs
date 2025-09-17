namespace NumbatWallet.SharedKernel.Primitives;

public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    protected AuditableEntity(TId id) : base(id)
    {
    }
}