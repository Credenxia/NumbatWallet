using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Repositories;

public interface IPersonRepository : IRepository<Person, Guid>
{
    Task<Person?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<Person?> GetByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> PhoneNumberExistsAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Person>> GetVerifiedPersonsAsync(CancellationToken cancellationToken = default);
}