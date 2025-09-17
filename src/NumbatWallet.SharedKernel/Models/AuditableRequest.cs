namespace NumbatWallet.SharedKernel.Models;

public abstract class AuditableRequest
{
    public string? RequestedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }
}