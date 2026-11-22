namespace EnterpriseResourceSchedulingSystem.Application.Dtos;

public enum ResourceFeeSaveResult
{
    Saved,
    OverlappingPeriod,
    DuplicateStart,
    ReferenceNotFound,
    IdentityTypeNotFound,
    FeeNotFound
}
