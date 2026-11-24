using EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;
using EnterpriseResourceSchedulingSystem.Application.Dtos;
using EnterpriseResourceSchedulingSystem.Application.Services;
using EnterpriseResourceSchedulingSystem.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseResourceSchedulingSystem.Web.Areas.Frontend.Controllers;

[Area("Frontend")]
[ApiController]
[Route("api/schedule")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ScheduleApiController(IResourceService resources, IUserService users, ScheduleService schedules, IResourceFeeService resourceFees) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<ScheduleFilterOptions>> GetOptions(CancellationToken cancellationToken)
    {
        var resourceOptions = await resources.GetResourceOptionsAsync(cancellationToken);
        var userOptions = await users.GetUserOptionsAsync(cancellationToken);
        return Ok(new ScheduleFilterOptions(resourceOptions, userOptions));
    }

    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots(DateOnly from, DateOnly to, [FromQuery] Guid[] resourceIds,
        [FromQuery] Guid[] userIds, CancellationToken ct, bool includeCancelled = false, bool includeUnbooked = false)
    {
        if (resourceIds.Length + userIds.Length > 1000)
            return BadRequest(new { message = "篩選條件過多，請縮小範圍。" });

        try
        {
            return Ok(await schedules.GetSlotsAsync(from, to, resourceIds, userIds, includeCancelled, includeUnbooked, ct));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("slots/{slotId:guid}/booking-users")]
    [Authorize(Policy = AuthorizationPolicies.BackendAccess)]
    public async Task<IActionResult> GetPermittedUsers(Guid slotId, CancellationToken ct)
        => Ok(await schedules.GetPermittedUsersAsync(slotId, ct));

    [HttpGet("slots/{slotId:guid}/bookings")]
    public async Task<IActionResult> GetBookings(Guid slotId, CancellationToken ct)
        => Ok(await schedules.GetBookingsAsync(slotId, ct));

    [HttpGet("slots/{slotId:guid}/booking-changes")]
    [Authorize(Policy = AuthorizationPolicies.BackendAccess)]
    public async Task<IActionResult> GetBookingChanges(Guid slotId, CancellationToken ct)
        => Ok(await schedules.GetBookingChangesAsync(slotId, ct));

    [HttpGet("resources/{resourceId:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid resourceId, DateOnly date, CancellationToken ct)
        => Ok(await schedules.GetVersionsAsync(resourceId, date, ct));

    [HttpGet("resources/{resourceId:guid}/versions/{resourceConfigId:guid}/fees")]
    public async Task<IActionResult> GetVersionFees(Guid resourceId, Guid resourceConfigId, CancellationToken ct)
    {
        var fees = await resourceFees.GetByResourceAsync(resourceId, resourceConfigId, ct);
        return fees is null ? NotFound() : Ok(fees.Select(fee => new { fee.IdentityName, fee.Fee }));
    }

    [HttpPost("slots")]
    [Authorize(Policy = AuthorizationPolicies.BackendAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSlot(CreateScheduleSlot request, CancellationToken ct)
    {
        try
        {
            var slotId = await schedules.CreateSlotAsync(request, CurrentActor(), ct);
            return StatusCode(StatusCodes.Status201Created, new { slotId });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ScheduleConflictException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPut("slots/{slotId:guid}/time")]
    [Authorize(Policy = AuthorizationPolicies.BackendAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RescheduleSlot(Guid slotId, RescheduleScheduleSlot request, CancellationToken ct)
    {
        try
        {
            await schedules.RescheduleSlotAsync(slotId, request, CurrentActor(), ct);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ScheduleConflictException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("slots/{slotId:guid}/capacity/{capacity:int}")]
    [Authorize(Policy = AuthorizationPolicies.BackendAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IncreaseCapacity(Guid slotId, int capacity, CancellationToken ct)
        => await schedules.IncreaseCapacityAsync(slotId, capacity, CurrentActor(), ct)
            ? NoContent()
            : Conflict(new { message = "只能增加尚未開始的開放時段名額，且不可超過設備設定上限。若名額已變更，請重新選取時段。" });

    [HttpPost("slots/{slotId:guid}/bookings/{userId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.BackendAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(Guid slotId, Guid userId, CancellationToken ct)
        => await schedules.BookAsync(slotId, userId, CurrentActor(), ct)
            ? NoContent()
            : Conflict(new { message = "無法預約。請確認時段尚未開始、有剩餘名額、人員有使用權限與適用費率，且沒有重疊預約。" });
    [HttpPost("slots/{slotId:guid}/bookings/{bookingId:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.BackendAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(Guid slotId, Guid bookingId, CancellationToken ct)
        => await schedules.CancelBookingAsync(slotId, bookingId, CurrentActor(), ct)
            ? NoContent()
            : Conflict(new { message = "找不到可取消的預約。" });

    [HttpPost("slots/{slotId:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.BackendAccess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSlot(Guid slotId, CancellationToken ct)
        => await schedules.CancelSlotAsync(slotId, CurrentActor(), ct)
            ? NoContent()
            : Conflict(new { message = "找不到可取消的時段。" });

    private string CurrentActor()
    {
        var name = User.Identity?.Name ?? "SYSTEM";
        return name.Length > 50 ? name[..50] : name;
    }
}
