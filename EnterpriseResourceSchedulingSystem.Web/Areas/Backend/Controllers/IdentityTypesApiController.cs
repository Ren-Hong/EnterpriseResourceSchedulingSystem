using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using EnterpriseResourceSchedulingSystem.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Backend.Controllers;

[Area("Backend")]
[ApiController]
[Route("api/identity-types")]
[Authorize(Policy = AuthorizationPolicies.BackendAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IdentityTypesApiController(IIdentityTypeService identityTypes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IdentityTypePage>> GetPage(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
        => Ok(await identityTypes.GetPageAsync(keyword, page, cancellationToken));

    [HttpGet("{identityTypeId:guid}")]
    public async Task<ActionResult<IdentityTypeListItem>> FindById(Guid identityTypeId, CancellationToken cancellationToken)
    {
        var item = await identityTypes.FindByIdAsync(identityTypeId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] SaveIdentityTypeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await identityTypes.CreateAsync(request.IdentityName, request.Description, CurrentActor(), cancellationToken);
            return created
                ? Ok(new { message = "身分別已新增。" })
                : Conflict(new { message = "此身分別名稱已存在。" });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{identityTypeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid identityTypeId, [FromBody] SaveIdentityTypeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await identityTypes.UpdateAsync(identityTypeId, request.IdentityName, request.Description, CurrentActor(), cancellationToken) switch
            {
                IdentityTypeUpdateResult.Updated => Ok(new { message = "身分別資料已更新。" }),
                IdentityTypeUpdateResult.NotFound => NotFound(new { message = "找不到這筆身分別資料。" }),
                IdentityTypeUpdateResult.DuplicateName => Conflict(new { message = "此身分別名稱已存在。" }),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{identityTypeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid identityTypeId, CancellationToken cancellationToken)
    {
        return await identityTypes.DeleteAsync(identityTypeId, cancellationToken) switch
        {
            IdentityTypeDeleteResult.Deleted => Ok(new { message = "身分別已刪除。" }),
            IdentityTypeDeleteResult.NotFound => NotFound(new { message = "找不到這筆身分別資料。" }),
            IdentityTypeDeleteResult.InUse => Conflict(new { message = "此身分別已被人員、資源費率或預約資料使用，無法刪除。" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private string CurrentActor() => User.Identity?.Name is { Length: > 0 } name ? name : "SYSTEM";
}
