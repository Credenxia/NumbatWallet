namespace NumbatWallet.SharedKernel.Enums;

public enum AuditEventType
{
    Create = 0,
    Read = 1,
    Update = 2,
    Delete = 3,
    Login = 4,
    Logout = 5,
    CredentialIssued = 6,
    CredentialVerified = 7,
    CredentialRevoked = 8
}