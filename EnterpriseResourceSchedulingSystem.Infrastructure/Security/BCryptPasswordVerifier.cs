using EnterpriseResourceSchedulingSystem.Application.Abstractions.Security;

namespace EnterpriseResourceSchedulingSystem.Infrastructure.Security;

public sealed class BCryptPasswordVerifier : IPasswordVerifier
{
    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
