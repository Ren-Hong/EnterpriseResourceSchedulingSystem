using EnterpriseResourceSchedulingSystem.Application.Dtos;

namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Services;

/// <summary>處理登入帳號的身分驗證。</summary>
public interface IAccountAuthenticationService
{
    /// <summary>驗證帳號密碼並記錄登入成功時間，驗證失敗時回傳 null。</summary>
    Task<AuthenticatedAccount?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken);
}
