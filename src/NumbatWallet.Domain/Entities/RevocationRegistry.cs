using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Domain.Aggregates;

public class RevocationRegistry : Entity<Guid>
{
    public RevocationRegistry() : base(Guid.NewGuid())
    {
    }

    public Guid IssuerId { get; set; }
    public string RegistryId { get; set; } = string.Empty;
    public string CredentialType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int MaxCredentials { get; set; }
    public int CurrentCredentials { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FullAt { get; set; }
}