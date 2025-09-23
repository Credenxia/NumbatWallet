using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Commands.Verifications;

namespace NumbatWallet.Application.Queries.Verifications;

// Query to get verification by ID
public record GetVerificationByIdQuery(
    Guid TenantId,
    Guid VerificationId) : IQuery<VerificationDto?>;

// Query to get verification history
public record GetVerificationHistoryQuery(
    Guid TenantId,
    string? VerifierDid,
    string? HolderDid,
    DateTime FromDate,
    DateTime ToDate,
    int Limit) : IQuery<IEnumerable<VerificationDto>>;

// Query to get verification request by ID
public record GetVerificationRequestByIdQuery(
    Guid TenantId,
    Guid RequestId) : IQuery<VerificationRequestDto?>;

// Query to get pending verification requests
public record GetPendingVerificationRequestsQuery(
    Guid TenantId,
    string? RequestorDid,
    int Limit) : IQuery<IEnumerable<VerificationRequestDto>>;

// DTOs
public class VerificationDto
{
    public Guid Id { get; set; }
    public string VerifierDid { get; set; } = string.Empty;
    public string HolderDid { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public bool IsValid { get; set; }
    public string[] VerifiedClaims { get; set; } = Array.Empty<string>();
    public string Purpose { get; set; } = string.Empty;
    public string? CredentialType { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}