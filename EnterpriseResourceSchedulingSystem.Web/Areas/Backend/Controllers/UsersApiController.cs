using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using EnterpriseResourceSchedulingSystem.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Backend.Controllers;

[Area("Backend")]
[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.BackendAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsersApiController(IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserPage>> GetPage(
        [FromQuery] string? keyword,
        [FromQuery] Guid? identityTypeId,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await users.GetPageAsync(keyword, identityTypeId, page, cancellationToken);
        return Ok(result);
    }

    [HttpGet("identity-types")]
    public async Task<ActionResult<IReadOnlyList<IdentityTypeOption>>> GetIdentityTypes(CancellationToken cancellationToken)
    {
        return Ok(await users.GetIdentityTypesAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        if (request.IdentityTypeId == Guid.Empty)
            return BadRequest(new { message = "請選擇身分別。" });

        try
        {
            await users.CreateAsync(request.DisplayName, request.IdentityTypeId, CurrentActor(), cancellationToken);
            return Ok(new { message = "人員已新增。" });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{userId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid userId, [FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        if (request.IdentityTypeId == Guid.Empty)
            return BadRequest(new { message = "請選擇身分別。" });

        try
        {
            var updated = await users.UpdateAsync(userId, request.DisplayName, request.IdentityTypeId, CurrentActor(), cancellationToken);
            return updated ? Ok(new { message = "人員資料已更新。" }) : NotFound(new { message = "找不到這筆人員資料。" });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{userId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        var deleted = await users.DeleteAsync(userId, cancellationToken);
        return deleted
            ? Ok(new { message = "人員已刪除。" })
            : Conflict(new { message = "人員不存在，或已被資源權限／預約資料引用，無法刪除。" });
    }

    private string CurrentActor() => User.Identity?.Name is { Length: > 0 } name ? name : "SYSTEM";
}
