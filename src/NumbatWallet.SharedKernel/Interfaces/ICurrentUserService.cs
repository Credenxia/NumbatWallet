namespace NumbatWallet.SharedKernel.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    string UserEmail { get; }
    IEnumerable<string> Roles { get; }
}