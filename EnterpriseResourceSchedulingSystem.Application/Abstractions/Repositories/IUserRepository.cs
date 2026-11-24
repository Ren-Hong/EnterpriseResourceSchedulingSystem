using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

/// <summary>讀寫人員資料並查詢相關身分別。</summary>
public interface IUserRepository
{
    /// <summary>取得前台排程人員篩選選項，包含姓名及身分別。</summary>
    Task<IReadOnlyList<ScheduleUserOption>> GetUserOptionsAsync(CancellationToken cancellationToken);

    /// <summary>計算符合姓名關鍵字及身分別條件的人員總數。</summary>
    Task<int> CountAsync(string? keyword, Guid? identityTypeId, CancellationToken cancellationToken);

    /// <summary>依姓名關鍵字及身分別分頁查詢人員。</summary>
    Task<IReadOnlyList<UserListItem>> GetPageAsync(
        string? keyword,
        Guid? identityTypeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>取得新增或編輯人員時可選的身分別。</summary>
    Task<IReadOnlyList<IdentityTypeOption>> GetIdentityTypesAsync(CancellationToken cancellationToken);

    /// <summary>依識別碼取得人員資料，查無資料時回傳 null。</summary>
    Task<UserListItem?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>新增具指定身分別的人員資料。</summary>
    Task CreateAsync(string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken);

    /// <summary>修改人員姓名與身分別，回傳是否更新成功。</summary>
    Task<bool> UpdateAsync(Guid userId, string displayName, Guid identityTypeId, string actor, CancellationToken cancellationToken);

    /// <summary>依識別碼刪除人員，回傳是否刪除成功。</summary>
    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken);
}
