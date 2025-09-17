namespace NumbatWallet.SharedKernel.Interfaces;

public interface IDateTimeService
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset LocalNow { get; }
    DateTimeOffset ToUtc(DateTimeOffset dateTime);
    DateTimeOffset ToLocal(DateTimeOffset dateTime);
}