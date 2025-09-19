using System;
using System.Collections.Generic;

namespace NumbatWallet.Application.DTOs;

public class WalletDto
{
    public required string Id { get; init; }
    public required string PersonId { get; init; }
    public required string PersonName { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public bool IsActive { get; init; }
    public bool IsSuspended { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int CredentialCount { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public class WalletSummaryDto
{
    public required string Id { get; init; }
    public required string PersonName { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public int CredentialCount { get; init; }
    public int ActiveCredentials { get; init; }
    public int ExpiredCredentials { get; init; }
    public DateTimeOffset LastActivity { get; init; }
}