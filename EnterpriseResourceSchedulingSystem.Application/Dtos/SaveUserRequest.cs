using System.ComponentModel.DataAnnotations;

namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public sealed class SaveUserRequest
{
    [Required, StringLength(100)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    public Guid IdentityTypeId { get; init; }
}
