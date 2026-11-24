namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Security;

/// <summary>定義密碼驗證功能。</summary>
public interface IPasswordVerifier
{
    /// <summary>驗證明文密碼是否符合儲存的密碼雜湊。</summary>
    bool Verify(string password, string passwordHash);
}
