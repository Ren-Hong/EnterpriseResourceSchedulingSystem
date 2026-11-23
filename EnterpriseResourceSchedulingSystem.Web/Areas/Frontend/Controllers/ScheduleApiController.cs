using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Frontend.Controllers;

[Area("Frontend")]
[ApiController]
[Route("api/schedule/options")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ScheduleApiController(IResourceService resources, IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ScheduleFilterOptions>> GetOptions(CancellationToken cancellationToken)
    {
        var resourceOptions = await resources.GetResourceOptionsAsync(cancellationToken);
        var userOptions = await users.GetUserOptionsAsync(cancellationToken);
        return Ok(new ScheduleFilterOptions(resourceOptions, userOptions));
    }
}
