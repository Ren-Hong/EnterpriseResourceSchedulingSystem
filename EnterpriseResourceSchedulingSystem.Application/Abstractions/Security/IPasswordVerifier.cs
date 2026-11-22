namespace EnterpriseResourceSchedulingSystem.Application.Abstractions.Security;

public interface IPasswordVerifier
{
    bool Verify(string password, string passwordHash);
}
