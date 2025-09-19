using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Domain.Entities;

public class SupportedCredentialType : Entity<Guid>
{
    public SupportedCredentialType() : base(Guid.NewGuid())
    {
    }

    public Guid IssuerId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string SchemaId { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}