using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Domain.Exceptions;
using RefalinaLaundry.Web.Models;

namespace RefalinaLaundry.Web.Controllers;

[Authorize(Roles = RoleNames.Operational)]
[Route("Customers")]
public sealed class CustomersController : AppController
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers)
    {
        _customers = customers;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerDto> customers = await _customers.ListAsync(search, cancellationToken);
        return View(new CustomerPageViewModel(customers, search, new CustomerFormModel()));
    }

    [HttpGet("Search")]
    public async Task<IActionResult> Search(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerDto> results = await _customers.SearchAsync(q ?? string.Empty, cancellationToken);
        return Json(results.Select(customer => new
        {
            customer.Id,
            customer.Name,
            customer.PhoneNumber,
            Label = customer.PhoneNumber is null ? customer.Name : $"{customer.Name} · {customer.PhoneNumber}",
        }));
    }

    [HttpPost("")]
    public async Task<IActionResult> Create(CustomerFormModel form, CancellationToken cancellationToken)
    {
        try
        {
            CustomerDto customer = await _customers.CreateAsync(form.ToInput(), cancellationToken);
            if (WantsJson())
            {
                return Json(customer);
            }

            TempData["Success"] = "Pelanggan berhasil ditambahkan.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ValidationException or DomainRuleException or ConflictException)
        {
            if (WantsJson())
            {
                return BadRequest(new { message = exception.Message });
            }

            AddApplicationError(exception);
            IReadOnlyList<CustomerDto> customers = await _customers.ListAsync(null, cancellationToken);
            return View(nameof(Index), new CustomerPageViewModel(customers, null, form));
        }
    }

    [HttpPost("{id:int}/Update")]
    public async Task<IActionResult> Update(int id, CustomerFormModel form, CancellationToken cancellationToken)
    {
        try
        {
            await _customers.UpdateAsync(id, form.ToInput(), cancellationToken);
            TempData["Success"] = "Data pelanggan diperbarui.";
        }
        catch (Exception exception) when (exception is ValidationException or DomainRuleException or ConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _customers.DeleteAsync(id, cancellationToken);
        TempData["Success"] = "Pelanggan dinonaktifkan.";
        return RedirectToAction(nameof(Index));
    }

    private bool WantsJson() =>
        Request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
}
