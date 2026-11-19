using EnterpriseResourceSchedulingSystem.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Backend.Controllers;

[Area("Backend")]
[Authorize(Policy = AuthorizationPolicies.BackendAccess)]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
