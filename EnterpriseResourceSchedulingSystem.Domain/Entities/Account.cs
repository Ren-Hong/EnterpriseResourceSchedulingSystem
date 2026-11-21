namespace EnterpriseResourceSchedulingSystem.Domain.Entities;

public sealed record Account(
    Guid AccountId,
    string Username,
    string PasswordHash,
    string Level)
{
    public const string AdministratorLevel = "管理員";
}
