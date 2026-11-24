using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Repositories;

/// <summary>讀寫身分別資料。</summary>
public interface IIdentityTypeRepository
{
    /// <summary>計算符合關鍵字的身分別總數，供分頁使用。</summary>
    Task<int> CountAsync(string? keyword, CancellationToken cancellationToken);

    /// <summary>依關鍵字分頁查詢身分別。</summary>
    Task<IReadOnlyList<IdentityTypeListItem>> GetPageAsync(
        string? keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>依識別碼取得身分別資料，查無資料時回傳 null。</summary>
    Task<IdentityTypeListItem?> FindByIdAsync(Guid identityTypeId, CancellationToken cancellationToken);

    /// <summary>新增身分別，回傳是否建立成功。</summary>
    Task<bool> CreateAsync(string identityName, string? description, string actor, CancellationToken cancellationToken);

    /// <summary>修改身分別名稱與說明，回傳更新結果。</summary>
    Task<IdentityTypeUpdateResult> UpdateAsync(
        Guid identityTypeId,
        string identityName,
        string? description,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>刪除身分別，回傳刪除成功、不存在或仍被引用等結果。</summary>
    Task<IdentityTypeDeleteResult> DeleteAsync(Guid identityTypeId, CancellationToken cancellationToken);
}
