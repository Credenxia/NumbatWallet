using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Data.Interceptors;

/// <summary>
/// Populates the deterministic search-token shadow columns (<c>email_search_token</c>,
/// <c>phone_search_token</c>) on <see cref="Person"/> rows before every save. Email and phone
/// are AES-256-GCM encrypted at rest (non-deterministic ciphertext), so exact-match lookups
/// (login, uniqueness checks) query these HMAC tokens instead — see
/// <see cref="IHmacSearchTokenService.GenerateEmailTokenAsync"/>.
///
/// Tokens are recomputed for every tracked (non-deleted) Person, not just Modified ones,
/// because email/phone live on owned value objects: replacing the owned instance can leave the
/// owner entry Unchanged. Assigning an identical token to an Unchanged entry is a no-op, so
/// this stays cheap (HMAC with a cached pepper) and never dirties clean rows.
/// </summary>
public sealed class SearchTokenInterceptor : SaveChangesInterceptor
{
    private readonly IHmacSearchTokenService _searchTokenService;

    public SearchTokenInterceptor(IHmacSearchTokenService searchTokenService)
    {
        _searchTokenService = searchTokenService;
    }

    public const string EmailSearchTokenProperty = "EmailSearchToken";
    public const string PhoneSearchTokenProperty = "PhoneSearchToken";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        // Sync saves are rare (seeder/tests use async); the pepper is cached after first use,
        // so blocking here is acceptable.
        ApplyTokensAsync(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await ApplyTokensAsync(eventData.Context).ConfigureAwait(false);
        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplyTokensAsync(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<Person>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Unchanged))
            {
                continue;
            }

            await SetTokenAsync(entry, EmailSearchTokenProperty, entry.Entity.Email?.Value, _searchTokenService.GenerateEmailTokenAsync)
                .ConfigureAwait(false);
            await SetTokenAsync(entry, PhoneSearchTokenProperty, entry.Entity.PhoneNumber?.Value, _searchTokenService.GeneratePhoneTokenAsync)
                .ConfigureAwait(false);
        }
    }

    private static async Task SetTokenAsync(
        EntityEntry<Person> entry,
        string propertyName,
        string? plaintext,
        Func<string, Task<string?>> tokenizer)
    {
        var token = string.IsNullOrWhiteSpace(plaintext)
            ? null
            : await tokenizer(plaintext).ConfigureAwait(false);

        var property = entry.Property<string?>(propertyName);
        if (!string.Equals(property.CurrentValue, token, StringComparison.Ordinal))
        {
            property.CurrentValue = token;
        }
    }
}
