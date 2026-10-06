using FluentAssertions;
using NSubstitute;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Application.Services;
using RefalinaLaundry.Application.Validation;
using RefalinaLaundry.Domain.Entities;

namespace RefalinaLaundry.Application.Tests;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesPhoneAndPersistsCustomer()
    {
        ICustomerRepository repository = Substitute.For<ICustomerRepository>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IClock clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc));
        Customer? added = null;
        repository.When(item => item.Add(Arg.Any<Customer>())).Do(call => added = call.Arg<Customer>());
        CustomerService service = new(repository, unitOfWork, clock, new CustomerInputValidator());

        await service.CreateAsync(new CustomerInput("Siti Aminah", "0812-3456-7890", null, null));

        added.Should().NotBeNull();
        added!.PhoneNumber.Should().Be("6281234567890");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateNormalizedPhone_ThrowsConflict()
    {
        ICustomerRepository repository = Substitute.For<ICustomerRepository>();
        repository.PhoneExistsAsync("6281234567890", null, Arg.Any<CancellationToken>()).Returns(true);
        CustomerService service = new(
            repository,
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IClock>(),
            new CustomerInputValidator());

        Func<Task> action = () => service.CreateAsync(new CustomerInput("Siti Aminah", "081234567890", null, null));

        await action.Should().ThrowAsync<ConflictException>().WithMessage("*sudah digunakan*");
    }
}
