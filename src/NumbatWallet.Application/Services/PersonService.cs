using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

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
        var persons = await _personRepository.FindAsync(p => p.Email.Value == email, cancellationToken);
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
        var person = Person.Create(
            dto.Email,
            dto.FirstName,
            dto.LastName,
            dto.PhoneNumber,
            dto.DateOfBirth
        );

        await _personRepository.AddAsync(person, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToDto(person);
    }

    public async Task<PersonDto> UpdateAsync(Guid id, UpdatePersonDto dto, CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(id, cancellationToken);
        if (person == null)
            throw new InvalidOperationException($"Person with ID {id} not found");

        if (dto.FirstName != null)
            person.UpdateName(dto.FirstName, dto.LastName ?? person.LastName.Value);

        if (dto.PhoneNumber != null)
            person.UpdatePhoneNumber(dto.PhoneNumber);

        await _personRepository.UpdateAsync(person, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToDto(person);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(id, cancellationToken);
        if (person == null)
            return false;

        await _personRepository.DeleteAsync(person, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<PersonDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var persons = await _personRepository.FindAsync(
            p => p.Email.Value.Contains(searchTerm) ||
                 p.FirstName.Value.Contains(searchTerm) ||
                 p.LastName.Value.Contains(searchTerm),
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
            FirstName = person.FirstName.Value,
            LastName = person.LastName.Value,
            PhoneNumber = person.PhoneNumber?.Value,
            DateOfBirth = person.DateOfBirth,
            CreatedAt = person.CreatedAt,
            UpdatedAt = person.UpdatedAt
        };
    }
}