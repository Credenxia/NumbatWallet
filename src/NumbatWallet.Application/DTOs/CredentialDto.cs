using System;
using System.Collections.Generic;

namespace NumbatWallet.Application.DTOs;

public class CredentialDto
{
    public required string Id { get; init; }
    public required string HolderId { get; init; }
    public required string IssuerId { get; init; }
    public required string Type { get; init; }
    public required Dictionary<string, object> CredentialSubject { get; init; }
    public DateTime IssuanceDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public required string Status { get; init; }
    public Dictionary<string, object>? Proof { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public bool IsRevoked { get; init; }
    public DateTime? RevocationDate { get; init; }
    public string? RevocationReason { get; init; }
}

public class CredentialSummaryDto
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string HolderName { get; init; }
    public required string IssuerName { get; init; }
    public DateTime IssuanceDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public required string Status { get; init; }
    public bool IsExpired { get; init; }
    public bool IsRevoked { get; init; }
}