using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateTimeOffset Now => DateTimeOffset.Now;

    public DateTime UtcNowDateTime => DateTime.UtcNow;

    public DateTime NowDateTime => DateTime.Now;
}