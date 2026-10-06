using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefalinaLaundry.Application.Abstractions;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Application.Contracts;
using RefalinaLaundry.Web.Models;

namespace RefalinaLaundry.Web.Controllers;

[Authorize(Roles = RoleNames.Operational)]
public sealed class HomeController : Controller
{
    private readonly IOrderService _orders;

    public HomeController(IOrderService orders)
    {
        _orders = orders;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        DashboardDto dashboard = await _orders.GetDashboardAsync(cancellationToken);
        return View(dashboard);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [AllowAnonymous]
    public IActionResult StatusCodePage(int code)
    {
        Response.StatusCode = code;
        ViewData["StatusCode"] = code;
        return View();
    }
}
