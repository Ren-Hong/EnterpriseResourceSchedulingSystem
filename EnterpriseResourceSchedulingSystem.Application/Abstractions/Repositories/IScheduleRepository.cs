using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

/// <summary>讀寫前台日曆時段及人員預約。</summary>
public interface IScheduleRepository
{
    /// <summary>依 UTC 查詢範圍、設備、人員及取消狀態載入日曆時段，每個時段回傳一筆並附上有效預約數。</summary>
    /// <remarks>空的設備或人員清單不會匹配任何時段。全選人員時可另外包含空白時段。</remarks>
    Task<IReadOnlyList<ScheduleSlot>> GetSlotsAsync(DateTime fromUtc, DateTime toUtc, Guid[] resources, Guid[] users, bool includeCancelled, bool includeUnbooked, CancellationToken ct);

    /// <summary>取得使用權限涵蓋整個時段且尚未預約該時段的人員，供預約下拉選單使用。</summary>
    Task<IReadOnlyList<ScheduleUserOption>> GetPermittedUsersAsync(Guid slotId, CancellationToken ct);

    /// <summary>取得指定時段的有效預約，包含預約識別碼、人員姓名與預約身分別。</summary>
    Task<IReadOnlyList<ScheduleBooking>> GetBookingsAsync(Guid slotId, CancellationToken ct);

    /// <summary>取得指定時段的預約與取消紀錄。</summary>
    Task<IReadOnlyList<ScheduleBookingChange>> GetBookingChangesAsync(Guid slotId, CancellationToken ct);

    /// <summary>在目前交易中鎖定設備主檔，讓同一設備的時段建立依序檢查重疊。</summary>
    Task<bool> LockResourceForSlotCreationAsync(Guid resourceId, CancellationToken ct);

    /// <summary>判斷設備是否已有與指定 UTC 時間範圍重疊的未取消時段。</summary>
    Task<bool> HasOverlapAsync(Guid resourceId, DateTime startUtc, DateTime endUtc, CancellationToken ct, Guid? excludedSlotId = null);

    Task<bool> RescheduleSlotAsync(Guid slotId, RescheduleScheduleSlot request, DateTime startUtc, DateTime endUtc, string actor, CancellationToken ct);

    /// <summary>建立空白時段，保存選定的設定版本，並回傳時段識別碼。</summary>
    Task<Guid> CreateSlotAsync(Guid resourceId, Guid configId, int capacity, DateTime startUtc, DateTime endUtc, string? note, string actor, CancellationToken ct);

    /// <summary>鎖定預約人員，避免同一人員的預約同時通過撞期檢查。</summary>
    Task<bool> LockBookingUserAsync(Guid userId, CancellationToken ct);

    /// <summary>在目前交易中鎖定時段，並讀取業務判斷所需的資料及資料庫 UTC 時間。</summary>
    Task<(Domain.Scheduling.ScheduleSlot Slot, DateTime UtcNow)?> GetSlotForUpdateAsync(Guid slotId, CancellationToken ct);

    /// <summary>增加尚未開始的開放時段名額，新增後的名額不得超過設備版本上限。</summary>
    Task<bool> IncreaseCapacityAsync(Guid slotId, int capacity, string actor, CancellationToken ct);

    /// <summary>檢查時段名額、人員使用權限、身分費率及撞期，符合條件時建立預約。</summary>
    Task<bool> BookAsync(Guid slotId, Guid userId, string actor, CancellationToken ct);

    /// <summary>取消指定時段中的單筆有效預約並記錄取消時間與操作者。</summary>
    Task<bool> CancelBookingAsync(Guid slotId, Guid bookingId, string actor, CancellationToken ct);

    /// <summary>取消指定時段及其有效預約，並保留取消紀錄。</summary>
    Task<bool> CancelSlotAsync(Guid slotId, string actor, CancellationToken ct);
}
