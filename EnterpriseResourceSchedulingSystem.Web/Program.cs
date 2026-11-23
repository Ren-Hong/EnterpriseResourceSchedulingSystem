using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Persistence;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Security;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Services;
using EnterpriseResourceSchedulingSystem.Domain.Entities;
using EnterpriseResourceSchedulingSystem.Infrastructure.Repositories;
using EnterpriseResourceSchedulingSystem.Infrastructure.Persistence;
using EnterpriseResourceSchedulingSystem.Infrastructure.Security;
using EnterpriseResourceSchedulingSystem.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<AccountCookieEvents>();
builder.Services.AddSingleton<IPasswordVerifier, BCryptPasswordVerifier>();

var connectionString = builder.Configuration.GetConnectionString("ErssDatabase"); //WEB專案中的 SECRET.JSON
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddScoped<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
    builder.Services.AddScoped<IAccountAuthenticationService, AccountAuthenticationService>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IIdentityTypeRepository, IdentityTypeRepository>();
    builder.Services.AddScoped<IIdentityTypeService, IdentityTypeService>();
    builder.Services.AddScoped<IUserResourceRepository, UserResourceRepository>();
    builder.Services.AddScoped<IUserResourceService, UserResourceService>();
    builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
    builder.Services.AddScoped<IResourceConfigRepository, ResourceConfigRepository>();
    builder.Services.AddScoped<IResourceService, ResourceService>();
    builder.Services.AddScoped<IResourceFeeRepository, ResourceFeeRepository>();
    builder.Services.AddScoped<IResourceFeeService, ResourceFeeService>();
}

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.EventsType = typeof(AccountCookieEvents);
        options.Cookie.Name = "EnterpriseResourceSchedulingSystem.Auth";
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.BackendAccess, policy =>
        policy.RequireAuthenticatedUser().RequireRole(Account.AdministratorLevel));
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapAreaControllerRoute(
    name: "auth",
    areaName: "Auth",
    pattern: "Login/{controller=Login}/{action=Index}")
    .WithStaticAssets();

app.MapAreaControllerRoute(
    name: "frontend",
    areaName: "Frontend",
    pattern: "Frontend/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapAreaControllerRoute(
    name: "backend",
    areaName: "Backend",
    pattern: "Backend/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
