using FluentValidation;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CustomerInput> _validator;

    public CustomerService(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CustomerInput> validator)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public Task<IReadOnlyList<CustomerDto>> ListAsync(string? search, CancellationToken cancellationToken = default) =>
        _customers.ListAsync(search, cancellationToken);

    public Task<IReadOnlyList<CustomerDto>> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2
            ? Task.FromResult<IReadOnlyList<CustomerDto>>([])
            : _customers.SearchAsync(query.Trim(), 10, cancellationToken);

    public async Task<CustomerDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        Customer customer = await FindAsync(id, cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerDto> CreateAsync(CustomerInput input, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(input, cancellationToken);
        Customer customer = new(input.Name, input.PhoneNumber, input.Address, input.Notes, _clock.UtcNow);
        await EnsurePhoneUniqueAsync(customer.PhoneNumber, null, cancellationToken);
        _customers.Add(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    public async Task UpdateAsync(int id, CustomerInput input, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(input, cancellationToken);
        Customer customer = await FindAsync(id, cancellationToken);
        customer.Update(input.Name, input.PhoneNumber, input.Address, input.Notes, _clock.UtcNow);
        await EnsurePhoneUniqueAsync(customer.PhoneNumber, id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        Customer customer = await FindAsync(id, cancellationToken);
        customer.SoftDelete(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Customer> FindAsync(int id, CancellationToken cancellationToken) =>
        await _customers.GetAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException("Pelanggan tidak ditemukan.");

    private async Task EnsurePhoneUniqueAsync(string? phoneNumber, int? excludingId, CancellationToken cancellationToken)
    {
        if (phoneNumber is not null && await _customers.PhoneExistsAsync(phoneNumber, excludingId, cancellationToken))
        {
            throw new ConflictException("Nomor HP sudah digunakan pelanggan lain.");
        }
    }

    private static CustomerDto Map(Customer customer) =>
        new(customer.Id, customer.Name, customer.PhoneNumber, customer.Address, customer.Notes);
}
