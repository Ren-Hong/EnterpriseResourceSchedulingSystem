USE [ERSS];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @IdentitySeed TABLE (IdentityName NVARCHAR(50) NOT NULL);
INSERT INTO @IdentitySeed (IdentityName)
VALUES (N'成人'), (N'兒童'), (N'學生'), (N'特殊身分');

INSERT INTO dbo.ERSSIDNTBL (NVCHIDENTITYNAME)
SELECT seed.IdentityName
FROM @IdentitySeed AS seed
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ERSSIDNTBL AS existing
    WHERE existing.NVCHIDENTITYNAME = seed.IdentityName
);

DECLARE @UserSeed TABLE
(
    DisplayName NVARCHAR(100) NOT NULL,
    IdentityName NVARCHAR(50) NOT NULL
);

INSERT INTO @UserSeed (DisplayName, IdentityName)
VALUES
    (N'測試人員01', N'成人'), (N'測試人員02', N'兒童'), (N'測試人員03', N'學生'), (N'測試人員04', N'特殊身分'),
    (N'測試人員05', N'成人'), (N'測試人員06', N'兒童'), (N'測試人員07', N'學生'), (N'測試人員08', N'特殊身分'),
    (N'測試人員09', N'成人'), (N'測試人員10', N'兒童'), (N'測試人員11', N'學生'), (N'測試人員12', N'特殊身分'),
    (N'測試人員13', N'成人'), (N'測試人員14', N'兒童'), (N'測試人員15', N'學生'), (N'測試人員16', N'特殊身分'),
    (N'測試人員17', N'成人'), (N'測試人員18', N'兒童'), (N'測試人員19', N'學生'), (N'測試人員20', N'特殊身分'),
    (N'測試人員21', N'成人'), (N'測試人員22', N'兒童'), (N'測試人員23', N'學生'), (N'測試人員24', N'特殊身分'),
    (N'測試人員25', N'成人');

INSERT INTO dbo.ERSSUSERTBL (UIDIDNTYPE, NVCHDISPLAYNAME, VCHCREATEBY)
SELECT identityType.UIDIDNTYPE, seed.DisplayName, N'SAMPLE'
FROM @UserSeed AS seed
INNER JOIN dbo.ERSSIDNTBL AS identityType
    ON identityType.NVCHIDENTITYNAME = seed.IdentityName
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ERSSUSERTBL AS existing
    WHERE existing.NVCHDISPLAYNAME = seed.DisplayName
);

COMMIT TRANSACTION;
GO
