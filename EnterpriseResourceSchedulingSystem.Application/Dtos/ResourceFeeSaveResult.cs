namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public enum ResourceFeeSaveResult
{
    Saved,
    DuplicateIdentity,
    ReferenceNotFound,
    IdentityTypeNotFound,
    FeeNotFound
}
