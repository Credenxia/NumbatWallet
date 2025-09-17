namespace NumbatWallet.SharedKernel.Exceptions;

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object id)
        : base("ENTITY_NOT_FOUND", $"Entity '{entityName}' with id '{id}' was not found.")
    {
        EntityName = entityName;
        EntityId = id;
    }

    public string EntityName { get; }
    public object EntityId { get; }
}