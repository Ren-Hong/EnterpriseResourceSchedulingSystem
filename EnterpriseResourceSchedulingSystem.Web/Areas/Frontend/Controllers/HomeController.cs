using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Frontend.Controllers;

[Area("Frontend")]
public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(ScheduleController.Index), "Schedule");
}
