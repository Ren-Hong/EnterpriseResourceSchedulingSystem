using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Frontend.Controllers;

[Area("Frontend")]
public sealed class ScheduleController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
