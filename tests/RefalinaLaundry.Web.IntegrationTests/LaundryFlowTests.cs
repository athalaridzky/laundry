using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RefalinaLaundry.Application.Common;
using RefalinaLaundry.Infrastructure.Persistence;

namespace RefalinaLaundry.Web.IntegrationTests;

public sealed partial class LaundryFlowTests : IClassFixture<LaundryWebFactory>
{
    private readonly LaundryWebFactory _factory;

    public LaundryFlowTests(LaundryWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnauthenticatedRequest_RedirectsToLogin()
    {
        using HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        HttpResponseMessage response = await client.GetAsync("/Orders");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().StartWith("/Account/Login");
    }

    [Fact]
    public async Task Cashier_CannotOpenOwnerPages_ButCanReadActiveServices()
    {
        using HttpClient client = CreateAuthenticatedClient(RoleNames.Cashier, _factory.CashierId);

        (await client.GetAsync("/Services")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/Reports")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/Settings")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/Services/Active")).StatusCode.Should().Be(HttpStatusCode.OK);
        string createPage = await client.GetStringAsync("/Orders/Create");
        createPage.Should().NotContain("Ambil dari timbangan").And.NotContain("data-read-scale");
        (await client.GetAsync("/Orders/Scale")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MainFlow_CreatePartiallyPaySettlePickupReprintSearchAndReport_Succeeds()
    {
        using HttpClient client = CreateAuthenticatedClient(RoleNames.Owner, _factory.OwnerId);
        string createToken = await GetAntiforgeryTokenAsync(client, "/Orders/Create");
        Dictionary<string, string> createForm = new()
        {
            ["__RequestVerificationToken"] = createToken,
            ["Form.CustomerId"] = _factory.CustomerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Form.CustomerDisplay"] = "Siti Integration",
            ["Form.Items[0].ServiceId"] = _factory.RegularServiceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Form.Items[0].Quantity"] = "2.5",
            ["Form.PaymentAmount"] = "10000",
            ["Form.PaymentMethod"] = "Cash",
            ["Form.CashTendered"] = "10000",
        };

        HttpResponseMessage created = await client.PostAsync("/Orders/Create", new FormUrlEncodedContent(createForm));

        created.StatusCode.Should().Be(HttpStatusCode.Redirect);
        string receiptLocation = created.Headers.Location!.OriginalString;
        Match idMatch = OrderIdRegex().Match(receiptLocation);
        idMatch.Success.Should().BeTrue(receiptLocation);
        long orderId = long.Parse(idMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        string receipt = await client.GetStringAsync(receiptLocation);
        receipt.Should().Contain("RL-").And.Contain("Rp 17.500").And.Contain("Rp 10.000");

        string detailToken = await GetAntiforgeryTokenAsync(client, $"/Orders/{orderId}");
        HttpResponseMessage settled = await client.PostAsync(
            $"/Orders/{orderId}/Payments",
            Form(detailToken,
                ("Payment.Amount", "7500"),
                ("Payment.Method", "Cash"),
                ("Payment.CashTendered", "10000")));
        settled.StatusCode.Should().Be(HttpStatusCode.Redirect);

        foreach (string status in new[] { "InProcess", "Done", "PickedUp" })
        {
            HttpResponseMessage changed = await client.PostAsync(
                $"/Orders/{orderId}/Status",
                Form(detailToken, ("status", status)));
            changed.StatusCode.Should().Be(HttpStatusCode.Redirect);
        }

        string detail = await client.GetStringAsync($"/Orders/{orderId}");
        detail.Should().Contain("Diambil").And.Contain("Lunas").And.Contain("Rp 0");

        HttpResponseMessage reprint = await client.PostAsync(
            $"/Orders/{orderId}/Receipt/Reprint",
            Form(detailToken));
        reprint.StatusCode.Should().Be(HttpStatusCode.Redirect);
        string copyReceipt = await client.GetStringAsync(reprint.Headers.Location!.OriginalString);
        copyReceipt.Should().Contain("SALINAN");

        string orderNumber;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            orderNumber = await dbContext.Orders.Where(order => order.Id == orderId).Select(order => order.OrderNumber).SingleAsync();
            (await dbContext.AuditLogs.CountAsync(log => log.Action == AuditActions.ReceiptReprinted && log.EntityId == orderId.ToString(System.Globalization.CultureInfo.InvariantCulture))).Should().Be(1);
        }

        string history = await client.GetStringAsync($"/Orders?search={WebUtility.UrlEncode(orderNumber)}");
        history.Should().Contain(orderNumber);

        DateOnly reportDate = DateOnly.ParseExact(
            orderNumber.Split('-')[1],
            "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture);
        string reportDateValue = reportDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        string report = await client.GetStringAsync($"/Reports?from={reportDateValue}&to={reportDateValue}");
        report.Should().Contain("Rincian pembayaran")
            .And.Contain(orderNumber)
            .And.Contain("from=" + reportDateValue)
            .And.Contain("to=" + reportDateValue);
        HttpResponseMessage pdf = await client.GetAsync(
            $"/Reports/Export?from={reportDateValue}&to={reportDateValue}&format=pdf");
        pdf.StatusCode.Should().Be(HttpStatusCode.OK);
        pdf.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        byte[] pdfContent = await pdf.Content.ReadAsByteArrayAsync();
        pdfContent.Should().HaveCountGreaterThan(500);
        System.Text.Encoding.ASCII.GetString(pdfContent, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Login_WithCorrectPassword_RedirectsToApplication()
    {
        using HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        string token = await GetAntiforgeryTokenAsync(client, "/Account/Login");

        HttpResponseMessage response = await client.PostAsync(
            "/Account/Login",
            Form(token,
                ("UserName", "test.owner"),
                ("Password", "Integration1!"),
                ("RememberMe", "false"),
                ("ReturnUrl", "/")));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be("/");
    }

    [Fact]
    public async Task Login_AfterFiveInvalidPasswords_LocksAccount()
    {
        using HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        string token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/Account/Login",
                Form(token, ("UserName", "test.cashier"), ("Password", "Salah1!")));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        Microsoft.AspNetCore.Identity.UserManager<RefalinaLaundry.Infrastructure.Identity.AppUser> userManager =
            scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<RefalinaLaundry.Infrastructure.Identity.AppUser>>();
        RefalinaLaundry.Infrastructure.Identity.AppUser cashier = await userManager.FindByIdAsync(
            _factory.CashierId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ?? throw new InvalidOperationException("Kasir integration tidak ditemukan.");
        (await userManager.IsLockedOutAsync(cashier)).Should().BeTrue();
    }

    [Fact]
    public async Task NewAccount_ChangesInitialPassword_AndCanLoginWithNewPassword()
    {
        string username = $"kasir{Guid.NewGuid():N}"[..18];
        const string initialPassword = "passwordawal";
        const string newPassword = "passwordbaru";
        using HttpClient ownerClient = CreateAuthenticatedClient(RoleNames.Owner, _factory.OwnerId);
        string settingsToken = await GetAntiforgeryTokenAsync(ownerClient, "/Settings");
        HttpResponseMessage created = await ownerClient.PostAsync(
            "/Settings/Users",
            Form(settingsToken,
                ("UserName", username),
                ("FullName", "Kasir Password Test"),
                ("Role", RoleNames.Cashier),
                ("Password", initialPassword)));
        created.StatusCode.Should().Be(HttpStatusCode.Redirect);

        int userId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            Microsoft.AspNetCore.Identity.UserManager<RefalinaLaundry.Infrastructure.Identity.AppUser> userManager =
                scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<RefalinaLaundry.Infrastructure.Identity.AppUser>>();
            RefalinaLaundry.Infrastructure.Identity.AppUser user = await userManager.FindByNameAsync(username)
                ?? throw new InvalidOperationException("Akun test tidak berhasil dibuat.");
            user.MustChangePassword.Should().BeTrue();
            (await userManager.CheckPasswordAsync(user, initialPassword)).Should().BeTrue();
            userId = user.Id;
        }

        using HttpClient userClient = CreateAuthenticatedClient(RoleNames.Cashier, userId);
        string passwordToken = await GetAntiforgeryTokenAsync(userClient, "/Account/ChangePassword");
        HttpResponseMessage changed = await userClient.PostAsync(
            "/Account/ChangePassword",
            Form(passwordToken,
                ("CurrentPassword", initialPassword),
                ("NewPassword", newPassword),
                ("ConfirmPassword", newPassword)));
        changed.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            Microsoft.AspNetCore.Identity.UserManager<RefalinaLaundry.Infrastructure.Identity.AppUser> userManager =
                scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<RefalinaLaundry.Infrastructure.Identity.AppUser>>();
            RefalinaLaundry.Infrastructure.Identity.AppUser user = await userManager.FindByNameAsync(username)
                ?? throw new InvalidOperationException("Akun test tidak ditemukan setelah ubah password.");
            user.MustChangePassword.Should().BeFalse();
            (await userManager.CheckPasswordAsync(user, initialPassword)).Should().BeFalse();
            (await userManager.CheckPasswordAsync(user, newPassword)).Should().BeTrue();
        }

        using HttpClient loginClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        string loginToken = await GetAntiforgeryTokenAsync(loginClient, "/Account/Login");
        HttpResponseMessage login = await loginClient.PostAsync(
            "/Account/Login",
            Form(loginToken, ("UserName", username), ("Password", newPassword)));
        login.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    private HttpClient CreateAuthenticatedClient(string role, int userId)
    {
        HttpClient client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return client;
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        string html = await client.GetStringAsync(path);
        Match match = AntiforgeryRegex().Match(html);
        match.Success.Should().BeTrue($"antiforgery token harus tersedia pada {path}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] values)
    {
        Dictionary<string, string> fields = new() { ["__RequestVerificationToken"] = token };
        foreach ((string key, string value) in values)
        {
            fields[key] = value;
        }

        return new FormUrlEncodedContent(fields);
    }

    [GeneratedRegex("/Orders/(\\d+)/Receipt", RegexOptions.IgnoreCase)]
    private static partial Regex OrderIdRegex();

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryRegex();
}
