using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset LocalNow => DateTimeOffset.Now;

    public DateTimeOffset ToUtc(DateTimeOffset dateTime)
    {
        return dateTime.ToUniversalTime();
    }

    public DateTimeOffset ToLocal(DateTimeOffset dateTime)
    {
        return dateTime.ToLocalTime();
    }
}