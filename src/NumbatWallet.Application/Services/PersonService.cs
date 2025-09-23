using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Services;

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PersonService(IPersonRepository personRepository, IUnitOfWork unitOfWork)
    {
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PersonDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(id, cancellationToken);
        return person != null ? MapToDto(person) : null;
    }

    public async Task<PersonDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // TODO: Implement specification pattern
        var allPersons = await _personRepository.GetAllAsync(cancellationToken);
        var persons = allPersons.Where(p => p.Email.Value == email);
        var person = persons.FirstOrDefault();
        return person != null ? MapToDto(person) : null;
    }

    public async Task<IEnumerable<PersonDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var persons = await _personRepository.GetAllAsync(cancellationToken);
        return persons.Select(MapToDto);
    }

    public async Task<PersonDto> CreateAsync(CreatePersonDto dto, CancellationToken cancellationToken = default)
    {
        // Convert DateTime to DateOnly if provided
        var dateOfBirth = dto.DateOfBirth.HasValue
            ? DateOnly.FromDateTime(dto.DateOfBirth.Value)
            : DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)); // Default age 25

        var email = Email.Create(dto.Email);
        var phoneNumber = string.IsNullOrEmpty(dto.PhoneNumber)
            ? PhoneNumber.Create("+61400000000") // Default Australian phone
            : PhoneNumber.Create(dto.PhoneNumber);

        var personResult = Person.Create(
            email,
            phoneNumber,
            dto.FirstName,
            dto.LastName,
            dateOfBirth
        );

        if (!personResult.IsSuccess)
        {
            throw new InvalidOperationException(personResult.Error.Message);
        }

        await _personRepository.AddAsync(personResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(personResult.Value);
    }

    public async Task<PersonDto> UpdateAsync(Guid id, UpdatePersonDto dto, CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(id, cancellationToken);
        if (person == null)
        {
            throw new InvalidOperationException($"Person with ID {id} not found");
        }

        // Note: Person entity doesn't have UpdateName or UpdatePhoneNumber methods
        // We would need to implement these methods or use a different approach
        // For now, we'll just return the existing person as-is
        // TODO: Implement person update logic in domain entity

        await _personRepository.UpdateAsync(person, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(person);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(id, cancellationToken);
        if (person == null)
        {
            return false;
        }

        await _personRepository.DeleteAsync(person, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<PersonDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var persons = await _personRepository.FindAsync(
            p => p.Email.Value.Contains(searchTerm) ||
                 p.FirstName.Contains(searchTerm) ||
                 p.LastName.Contains(searchTerm),
            cancellationToken
        );

        return persons.Select(MapToDto);
    }

    private static PersonDto MapToDto(Person person)
    {
        return new PersonDto
        {
            Id = person.Id,
            Email = person.Email.Value,
            FirstName = person.FirstName,
            LastName = person.LastName,
            PhoneNumber = person.PhoneNumber?.Value ?? string.Empty,
            DateOfBirth = person.DateOfBirth,
            ExternalId = person.ExternalId,
            EmailVerificationStatus = person.EmailVerificationStatus.ToString(),
            PhoneVerificationStatus = person.PhoneVerificationStatus.ToString(),
            IsVerified = person.IsVerified,
            Status = person.Status.ToString(),
            CreatedAt = person.CreatedAt,
            UpdatedAt = person.CreatedAt // Person doesn't have UpdatedAt, using CreatedAt
        };
    }

    public async Task<bool> VerifyIdentityAsync(Guid personId, IdentityVerificationDto verificationData, CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(personId, cancellationToken);
        if (person == null)
        {
            return false;
        }

        // TODO: Implement actual identity verification with external service
        // For now, just mark the person as verified
        person.MarkAsVerified();

        await _personRepository.UpdateAsync(person, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
