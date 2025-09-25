using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Web.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for Credential
/// </summary>
public class CredentialType : ObjectType<CredentialDto>
{
    protected override void Configure(IObjectTypeDescriptor<CredentialDto> descriptor)
    {
        descriptor.Name("Credential");
        descriptor.Description("A verifiable credential issued to a holder");

        descriptor.Field(c => c.Id)
            .Type<NonNullType<IdType>>()
            .Description("The unique identifier of the credential");

        descriptor.Field(c => c.HolderId)
            .Description("The identifier of the credential holder");

        descriptor.Field(c => c.IssuerId)
            .Description("The identifier of the credential issuer");

        descriptor.Field(c => c.Type)
            .Description("The type of credential (e.g., DriverLicense, ProofOfAge)");

        descriptor.Field(c => c.CredentialSubject)
            .Description("The claims contained in the credential");

        descriptor.Field(c => c.IssuanceDate)
            .Description("When the credential was issued");

        descriptor.Field(c => c.ExpirationDate)
            .Description("When the credential expires (if applicable)");

        descriptor.Field(c => c.Status)
            .Description("The current status of the credential (Active, Revoked, Expired)");

        descriptor.Field(c => c.IsRevoked)
            .Description("Whether the credential has been revoked");

        descriptor.Field(c => c.RevocationDate)
            .Description("When the credential was revoked (if applicable)");

        descriptor.Field(c => c.RevocationReason)
            .Description("The reason for revocation (if applicable)");

        descriptor.Field(c => c.Proof)
            .Description("Cryptographic proof of the credential");

        descriptor.Field(c => c.Metadata)
            .Description("Additional metadata about the credential");

        // Add computed field for expiration status
        descriptor.Field("isExpired")
            .Type<NonNullType<BooleanType>>()
            .Description("Whether the credential is currently expired")
            .Resolve(context =>
            {
                var credential = context.Parent<CredentialDto>();
                return credential.ExpirationDate.HasValue &&
                       credential.ExpirationDate.Value < DateTime.UtcNow;
            });

        // Add computed field for validity
        descriptor.Field("isValid")
            .Type<NonNullType<BooleanType>>()
            .Description("Whether the credential is currently valid")
            .Resolve(context =>
            {
                var credential = context.Parent<CredentialDto>();
                var isExpired = credential.ExpirationDate.HasValue &&
                               credential.ExpirationDate.Value < DateTime.UtcNow;
                return !credential.IsRevoked && !isExpired && credential.Status == "Active";
            });
    }
}

/// <summary>
/// Input type for issuing a credential
/// </summary>
public class IssueCredentialInput
{
    public required string HolderId { get; set; }
    public required string Type { get; set; }
    public required Dictionary<string, object> CredentialSubject { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Input type for verifying a credential
/// </summary>
public class VerifyCredentialInput
{
    public required string CredentialId { get; set; }
    public string? CredentialData { get; set; }
    public bool CheckRevocation { get; set; } = true;
    public bool CheckExpiry { get; set; } = true;
    public bool CheckSignature { get; set; } = true;
    public bool? CheckSchema { get; set; }
    public bool? RequireTrustChain { get; set; }
}

/// <summary>
/// Input type for revoking a credential
/// </summary>
public class RevokeCredentialInput
{
    public required string CredentialId { get; set; }
    public required string Reason { get; set; }
}

/// <summary>
/// Type for credential verification result
/// </summary>
public class VerificationResultType : ObjectType<VerificationResultDto>
{
    protected override void Configure(IObjectTypeDescriptor<VerificationResultDto> descriptor)
    {
        descriptor.Name("VerificationResult");
        descriptor.Description("The result of credential verification");

        descriptor.Field(v => v.IsValid)
            .Description("Whether the credential is valid");

        descriptor.Field(v => v.VerifiedAt)
            .Description("When the verification was performed");

        descriptor.Field(v => v.ErrorMessage)
            .Description("Error message if validation failed");

        descriptor.Field(v => v.Checks)
            .Description("Details of verification checks performed");

        descriptor.Field(v => v.Claims)
            .Description("Claims extracted from the credential");
    }
}

/// <summary>
/// Type for issuance
/// </summary>
public class IssuanceType : ObjectType<IssuanceDto>
{
    protected override void Configure(IObjectTypeDescriptor<IssuanceDto> descriptor)
    {
        descriptor.Name("Issuance");
        descriptor.Description("A credential issuance request");

        descriptor.Field(i => i.Id)
            .Type<NonNullType<IdType>>()
            .Description("The unique identifier of the issuance");

        descriptor.Field(i => i.CredentialType)
            .Description("The type of credential being issued");

        descriptor.Field(i => i.RequesterId)
            .Description("The ID of the requester");

        descriptor.Field(i => i.WalletId)
            .Description("The wallet ID for the credential");

        descriptor.Field(i => i.Status)
            .Description("The current status of the issuance");

        descriptor.Field(i => i.RequiredDocuments)
            .Description("List of required documents for issuance");

        descriptor.Field(i => i.AdditionalData)
            .Description("Additional data for the issuance");

        descriptor.Field(i => i.CreatedAt)
            .Description("When the issuance was created");

        descriptor.Field(i => i.ApprovedAt)
            .Description("When the issuance was approved");

        descriptor.Field(i => i.RejectedAt)
            .Description("When the issuance was rejected");

        descriptor.Field(i => i.CompletedAt)
            .Description("When the issuance was completed");

        descriptor.Field(i => i.ApprovedBy)
            .Description("Who approved the issuance");

        descriptor.Field(i => i.RejectedBy)
            .Description("Who rejected the issuance");

        descriptor.Field(i => i.CompletedBy)
            .Description("Who completed the issuance");

        descriptor.Field(i => i.RejectionReason)
            .Description("Reason for rejection");

        descriptor.Field(i => i.Comments)
            .Description("Comments about the issuance");

        descriptor.Field(i => i.CredentialId)
            .Description("The ID of the issued credential");
    }
}

/// <summary>
/// Input type for creating an issuance
/// </summary>
public class CreateIssuanceInput
{
    public required string CredentialType { get; set; }
    public required Guid WalletId { get; set; }
    public List<string>? RequiredDocuments { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Input type for approving an issuance
/// </summary>
public class ApproveIssuanceInput
{
    public required Guid IssuanceId { get; set; }
    public string? Comments { get; set; }
}

/// <summary>
/// Input type for rejecting an issuance
/// </summary>
public class RejectIssuanceInput
{
    public required Guid IssuanceId { get; set; }
    public required string Reason { get; set; }
}

/// <summary>
/// Enum for credential status
/// </summary>
public enum CredentialStatus
{
    Active,
    Revoked,
    Expired,
    Suspended,
    Pending
}

/// <summary>
/// Enum for issuance status
/// </summary>
public enum IssuanceStatus
{
    Pending,
    Approved,
    Rejected,
    Completed,
    Cancelled
}