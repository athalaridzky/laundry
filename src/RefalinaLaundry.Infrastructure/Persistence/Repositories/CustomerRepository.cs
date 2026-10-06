using Microsoft.EntityFrameworkCore;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Customer?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Customers.SingleOrDefaultAsync(customer => customer.Id == id && !customer.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<CustomerDto>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Customer> query = _dbContext.Customers.AsNoTracking().Where(customer => !customer.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(customer =>
                customer.Name.StartsWith(term) ||
                (customer.PhoneNumber != null && customer.PhoneNumber.StartsWith(term)));
        }

        return await query
            .OrderBy(customer => customer.Name)
            .Select(customer => new CustomerDto(
                customer.Id,
                customer.Name,
                customer.PhoneNumber,
                customer.Address,
                customer.Notes,
                customer.Orders.Count))
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        string term = query.Trim();
        string phoneTerm = NormalizePhoneSearch(term);
        return await _dbContext.Customers
            .AsNoTracking()
            .Where(customer =>
                !customer.IsDeleted &&
                (customer.Name.StartsWith(term) ||
                (customer.PhoneNumber != null && customer.PhoneNumber.StartsWith(phoneTerm))))
            .OrderBy(customer => customer.Name.StartsWith(term) ? 0 : 1)
            .ThenBy(customer => customer.Name)
            .Take(limit)
            .Select(customer => new CustomerDto(
                customer.Id,
                customer.Name,
                customer.PhoneNumber,
                customer.Address,
                customer.Notes,
                customer.Orders.Count))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> PhoneExistsAsync(
        string phoneNumber,
        int? excludingId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Customers.AnyAsync(
            customer => !customer.IsDeleted && customer.PhoneNumber == phoneNumber && (!excludingId.HasValue || customer.Id != excludingId.Value),
            cancellationToken);

    public void Add(Customer customer) => _dbContext.Customers.Add(customer);

    private static string NormalizePhoneSearch(string value)
    {
        string digits = new(value.Where(char.IsDigit).ToArray());
        return digits.StartsWith('0') ? $"62{digits[1..]}" : digits;
    }
}
