using FluentValidation;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Application.Services;

public sealed class ServiceCatalogService : IServiceCatalogService
{
    private readonly IServiceRepository _services;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<ServiceInput> _validator;

    public ServiceCatalogService(
        IServiceRepository services,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<ServiceInput> validator)
    {
        _services = services;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public Task<IReadOnlyList<ServiceDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default) =>
        _services.ListAsync(activeOnly, cancellationToken);

    public async Task<ServiceDto> GetAsync(int id, CancellationToken cancellationToken = default) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<ServiceDto> CreateAsync(ServiceInput input, int userId, CancellationToken cancellationToken = default)
    {
        await ValidateUniqueAsync(input, null, cancellationToken);
        Service service = new(input.Name, input.Category, input.UnitPrice, input.DurationDays, input.IsActive, _clock.UtcNow);
        _services.Add(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        AddAudit(service.Id, userId, $"Layanan dibuat dengan harga {service.UnitPrice:0.00}.");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(service);
    }

    public async Task UpdateAsync(int id, ServiceInput input, int userId, CancellationToken cancellationToken = default)
    {
        await ValidateUniqueAsync(input, id, cancellationToken);
        Service service = await FindAsync(id, cancellationToken);
        decimal previousPrice = service.UnitPrice;
        service.Update(input.Name, input.Category, input.UnitPrice, input.DurationDays, input.IsActive, _clock.UtcNow);
        if (previousPrice != service.UnitPrice)
        {
            AddAudit(id, userId, $"Harga berubah dari {previousPrice:0.00} menjadi {service.UnitPrice:0.00}.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        Service service = await FindAsync(id, cancellationToken);
        service.SoftDelete(_clock.UtcNow);
        AddAudit(id, userId, "Layanan dinonaktifkan dan dihapus secara lunak.");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateUniqueAsync(ServiceInput input, int? excludingId, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(input, cancellationToken);
        if (await _services.NameExistsAsync(input.Name.Trim(), excludingId, cancellationToken))
        {
            throw new ConflictException("Nama layanan sudah digunakan.");
        }
    }

    private async Task<Service> FindAsync(int id, CancellationToken cancellationToken) =>
        await _services.GetAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException("Layanan tidak ditemukan.");

    private void AddAudit(int serviceId, int userId, string detail) =>
        _auditLogs.Add(new AuditLog(
            userId,
            AuditActions.ServicePriceChanged,
            nameof(Service),
            serviceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            detail,
            _clock.UtcNow));

    private static ServiceDto Map(Service service) =>
        new(service.Id, service.Name, service.Category, service.UnitPrice, service.DurationDays, service.IsActive);
}
