using System.Security.Claims;
using System.Text;
using EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;
using EnterpriseResourceSchedulingSystem.Web.Areas.Auth.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Auth.Controllers;

[Area("Auth")]
[AllowAnonymous]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class LoginController(IAccountRepository? accounts = null) : Controller
{
    [HttpGet]
    public IActionResult Index(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectAfterLogin(returnUrl);
        }

        return View(new LoginInput { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        if (accounts is null)
        {
            ModelState.AddModelError(string.Empty, "登入服務尚未設定。");
            return View(input);
        }

        var account = await accounts.FindByUsernameAsync(input.Username.Trim(), cancellationToken);
        if (account is not { IsActive: true } ||
            Encoding.UTF8.GetByteCount(input.Password) > 72 ||
            !BCrypt.Net.BCrypt.Verify(input.Password, account.PasswordHash) ||
            !await accounts.RecordSuccessfulLoginAsync(account, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "帳號或密碼錯誤。");
            return View(input);
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, account.UserId.ToString()),
                new Claim(ClaimTypes.Name, account.Username),
                new Claim(ClaimTypes.Role, account.Level)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectAfterLogin(input.ReturnUrl);
    }

    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home", new { area = "Frontend" });
    }
}
