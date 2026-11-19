namespace EnterpriseResourceSchedulingSystem.Domain.Entities;

public sealed record Account(Guid UserId, string Username, string PasswordHash, bool IsActive, string Level)
{
    public const string AdministratorLevel = "管理員";
}
