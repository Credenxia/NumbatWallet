namespace NumbatWallet.SharedKernel.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset Now { get; }
    DateTimeOffset UtcNow { get; }
}