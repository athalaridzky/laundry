using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RefalinaLaundry.Application;
using RefalinaLaundry.Infrastructure;
using RefalinaLaundry.Infrastructure.Identity;
using RefalinaLaundry.Infrastructure.Persistence;
using RefalinaLaundry.Web.Infrastructure;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "RefalinaLaundry.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(10);
    options.SlidingExpiration = true;
});

CultureInfo indonesianCulture = CultureInfo.GetCultureInfo("id-ID");
CultureInfo.DefaultThreadCurrentCulture = indonesianCulture;
CultureInfo.DefaultThreadCurrentUICulture = indonesianCulture;

WebApplication app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        !context.Request.Path.StartsWithSegments("/Account/ChangePassword") &&
        !context.Request.Path.StartsWithSegments("/Account/Logout"))
    {
        UserManager<AppUser> userManager = context.RequestServices.GetRequiredService<UserManager<AppUser>>();
        AppUser? user = await userManager.GetUserAsync(context.User);
        if (user is null || !user.IsActive)
        {
            SignInManager<AppUser> signInManager = context.RequestServices.GetRequiredService<SignInManager<AppUser>>();
            await signInManager.SignOutAsync();
            context.Response.Redirect("/Account/Login");
            return;
        }

        if (user.MustChangePassword)
        {
            context.Response.Redirect("/Account/ChangePassword");
            return;
        }
    }

    await next(context);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (!app.Configuration.GetValue<bool>("SkipDatabaseInitialization"))
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    InitialDataSeeder seeder = scope.ServiceProvider.GetRequiredService<InitialDataSeeder>();
    await seeder.InitializeAsync(app.Environment.IsDevelopment());
    if (args.Any(argument => string.Equals(argument, "--reset-owner-password", StringComparison.OrdinalIgnoreCase)))
    {
        await seeder.ResetOwnerPasswordAsync();
    }
}

bool commandOnly = args.Any(argument =>
    string.Equals(argument, "--seed-only", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(argument, "--reset-owner-password", StringComparison.OrdinalIgnoreCase));
if (!commandOnly)
{
    await app.RunAsync();
}

public partial class Program
{
}
