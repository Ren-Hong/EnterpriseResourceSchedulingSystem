using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using EnterpriseResourceSchedulingSystem.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Backend.Controllers;

[Area("Backend")]
[ApiController]
[Route("api/resources")]
[Authorize(Policy = AuthorizationPolicies.BackendAccess)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ResourcesApiController(IResourceService resources, IResourceFeeService resourceFees) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResourcePage>> GetPage(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
        => Ok(await resources.GetPageAsync(keyword, page, cancellationToken));

    [HttpGet("{resourceId:guid}")]
    public async Task<ActionResult<ResourceListItem>> FindById(Guid resourceId, CancellationToken cancellationToken)
    {
        var item = await resources.FindByIdAsync(resourceId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{resourceId:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<ResourceVersionListItem>>> GetVersions(Guid resourceId, CancellationToken cancellationToken)
    {
        if (await resources.FindByIdAsync(resourceId, cancellationToken) is null) return NotFound();
        return Ok(await resources.GetVersionsAsync(resourceId, cancellationToken));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] SaveResourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await resources.CreateAsync(request.ResourceName, request.Description, request.MaxUsers, CurrentActor(), cancellationToken);
            return result switch
            {
                ResourceSaveResult.Saved => Ok(new { message = "設備／資源已新增。" }),
                ResourceSaveResult.DuplicateName => Conflict(new { message = "此設備／資源名稱已存在。" }),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{resourceId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid resourceId, [FromBody] SaveResourceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await resources.UpdateAsync(resourceId, request.ResourceName, request.Description, request.MaxUsers, request.EffectiveStart, request.EffectiveEnd, CurrentActor(), cancellationToken) switch
            {
                ResourceUpdateResult.Updated => Ok(new { message = "設備／資源資料已更新。" }),
                ResourceUpdateResult.NotFound => NotFound(new { message = "找不到這筆設備／資源資料。" }),
                ResourceUpdateResult.DuplicateName => Conflict(new { message = "此設備／資源名稱已存在。" }),
                ResourceUpdateResult.InvalidEffectiveStart => Conflict(new { message = "新版本生效日起必須晚於目前版本的生效起日。" }),
                ResourceUpdateResult.InvalidEffectivePeriod => BadRequest(new { message = "新版本生效迄日不可早於生效起日。" }),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{resourceId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid resourceId, CancellationToken cancellationToken)
    {
        return await resources.DeleteAsync(resourceId, cancellationToken) switch
        {
            ResourceDeleteResult.Deleted => Ok(new { message = "設備／資源已刪除。" }),
            ResourceDeleteResult.NotFound => NotFound(new { message = "找不到這筆設備／資源資料。" }),
            ResourceDeleteResult.InUse => Conflict(new { message = "此設備／資源已被費率、人員權限或排程資料使用，無法刪除。" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{resourceId:guid}/fees")]
    public async Task<ActionResult<IReadOnlyList<ResourceFeeListItem>>> GetFees(Guid resourceId, [FromQuery] Guid? resourceConfigId, CancellationToken cancellationToken)
    {
        var result = await resourceFees.GetByResourceAsync(resourceId, resourceConfigId, cancellationToken);
        return result is null ? NotFound(new { message = "找不到這筆設備／資源資料。" }) : Ok(result);
    }

    [HttpPost("{resourceId:guid}/versions/{resourceConfigId:guid}/fees")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFee(Guid resourceId, Guid resourceConfigId, [FromBody] SaveResourceFeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return FeeSaveResponse(await resourceFees.CreateAsync(resourceId, resourceConfigId, request, CurrentActor(), cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{resourceId:guid}/versions/{resourceConfigId:guid}/fees/{resourceFeeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFee(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, [FromBody] SaveResourceFeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return FeeSaveResponse(await resourceFees.UpdateAsync(resourceId, resourceConfigId, resourceFeeId, request, CurrentActor(), cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{resourceId:guid}/versions/{resourceConfigId:guid}/fees/{resourceFeeId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFee(Guid resourceId, Guid resourceConfigId, Guid resourceFeeId, CancellationToken cancellationToken)
        => await resourceFees.DeleteAsync(resourceId, resourceConfigId, resourceFeeId, cancellationToken) switch
        {
            ResourceFeeDeleteResult.Deleted => Ok(new { message = "身分費率已刪除。" }),
            ResourceFeeDeleteResult.NotFound => NotFound(new { message = "找不到這筆身分費率。" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };

    private IActionResult FeeSaveResponse(ResourceFeeSaveResult result)
        => result switch
        {
            ResourceFeeSaveResult.Saved => Ok(new { message = "身分費率已儲存。" }),
            ResourceFeeSaveResult.DuplicateIdentity => Conflict(new { message = "此設定版本已存在該身分別費率。" }),
            ResourceFeeSaveResult.IdentityTypeNotFound => NotFound(new { message = "找不到所選身分別。" }),
            ResourceFeeSaveResult.ReferenceNotFound => NotFound(new { message = "設備／資源或身分別已不存在，請重新載入。" }),
            ResourceFeeSaveResult.FeeNotFound => NotFound(new { message = "找不到這筆身分費率。" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };

    private string CurrentActor() => User.Identity?.Name is { Length: > 0 } name ? name : "SYSTEM";
}



