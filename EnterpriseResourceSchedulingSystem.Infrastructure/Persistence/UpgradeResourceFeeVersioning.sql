/* Move fee validity from individual rows to resource configuration versions.
   Run once after InitializeErss.sql and UpgradeSchedulingSchema.sql. Safe to rerun. */
USE [ERSS];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ERSSRESFEETBL', N'U') IS NULL OR OBJECT_ID(N'dbo.ERSSRESCFGTBL', N'U') IS NULL
    THROW 50100, 'Run InitializeErss.sql and UpgradeSchedulingSchema.sql first.', 1;

IF COL_LENGTH(N'dbo.ERSSRESFEETBL', N'DTMEFFECTSTART') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ERSSRESCFGTBL') AND name = N'CK_ERSSRESCFGTBL_EFFECTIVE')
        ALTER TABLE dbo.ERSSRESCFGTBL DROP CONSTRAINT CK_ERSSRESCFGTBL_EFFECTIVE;

    /* A configuration can only absorb one common validity period. Refuse any
       shape that would silently discard individual rate history. */
    /* Run the legacy-period migration as dynamic SQL so this script can be
       rerun after those columns have already been removed. */
    EXEC sys.sp_executesql N'
        IF EXISTS
        (
            SELECT UIDRESCFGID FROM dbo.ERSSRESFEETBL
            GROUP BY UIDRESCFGID, UIDIDNTYPE HAVING COUNT(*) > 1
        )
            THROW 50101, ''A resource version has multiple historical rates for one identity. Split these periods into separate resource versions before rerunning.'', 1;

        IF EXISTS
        (
            SELECT UIDRESCFGID FROM dbo.ERSSRESFEETBL GROUP BY UIDRESCFGID
            HAVING MIN(DTMEFFECTSTART) <> MAX(DTMEFFECTSTART)
                OR MIN(DTMEFFECTEND) <> MAX(DTMEFFECTEND)
        )
            THROW 50102, ''Fee periods differ inside a resource version. Align the fee periods or split them into resource versions before rerunning.'', 1;

        ;WITH FeePeriods AS
        (
            SELECT UIDRESCFGID, MIN(DTMEFFECTSTART) AS DTMEFFECTSTART, MAX(DTMEFFECTEND) AS DTMEFFECTEND
            FROM dbo.ERSSRESFEETBL GROUP BY UIDRESCFGID
        )
        UPDATE config
        SET config.DTMEFFECTSTART = periods.DTMEFFECTSTART,
            config.DTMEFFECTEND = periods.DTMEFFECTEND
        FROM dbo.ERSSRESCFGTBL AS config
        JOIN FeePeriods AS periods ON periods.UIDRESCFGID = config.UIDRESCFGID;

        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N''dbo.ERSSRESFEETBL'') AND name = N''IX_ERSSRESFEETBL_CFG_IDN'')
            DROP INDEX IX_ERSSRESFEETBL_CFG_IDN ON dbo.ERSSRESFEETBL;
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N''dbo.ERSSRESFEETBL'') AND name = N''UQ_ERSSRESFEETBL_CFG_RATESTART'')
            DROP INDEX UQ_ERSSRESFEETBL_CFG_RATESTART ON dbo.ERSSRESFEETBL;
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N''dbo.ERSSRESFEETBL'') AND name = N''CK_ERSSRESFEETBL_EFFECTIVE'')
            ALTER TABLE dbo.ERSSRESFEETBL DROP CONSTRAINT CK_ERSSRESFEETBL_EFFECTIVE;
        ALTER TABLE dbo.ERSSRESFEETBL DROP COLUMN DTMEFFECTSTART, DTMEFFECTEND;';
END;

/* Every version has an explicit end date; use 9999-12-31 for the open-ended version. */
UPDATE dbo.ERSSRESCFGTBL SET DTMEFFECTEND = CONVERT(date, '99991231', 112) WHERE DTMEFFECTEND IS NULL;

/* SQL Server will not change a column type while its default constraint is attached. */
DECLARE @StartDefault sysname;
SELECT @StartDefault = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ERSSRESCFGTBL') AND c.name = N'DTMEFFECTSTART';
IF @StartDefault IS NOT NULL
BEGIN
    DECLARE @DropStartDefault nvarchar(500) = N'ALTER TABLE dbo.ERSSRESCFGTBL DROP CONSTRAINT ' + QUOTENAME(@StartDefault);
    EXEC sys.sp_executesql @DropStartDefault;
END;
DECLARE @EndDefault sysname;
SELECT @EndDefault = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ERSSRESCFGTBL') AND c.name = N'DTMEFFECTEND';
IF @EndDefault IS NOT NULL
BEGIN
    DECLARE @DropEndDefault nvarchar(500) = N'ALTER TABLE dbo.ERSSRESCFGTBL DROP CONSTRAINT ' + QUOTENAME(@EndDefault);
    EXEC sys.sp_executesql @DropEndDefault;
END;
/* Make config bounds calendar dates and allow an inclusive, one-day version. */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ERSSRESCFGTBL') AND name = N'CK_ERSSRESCFGTBL_EFFECTIVE')
    ALTER TABLE dbo.ERSSRESCFGTBL DROP CONSTRAINT CK_ERSSRESCFGTBL_EFFECTIVE;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ERSSRESCFGTBL') AND name = N'DTMEFFECTSTART' AND system_type_id <> TYPE_ID(N'date'))
    ALTER TABLE dbo.ERSSRESCFGTBL ALTER COLUMN DTMEFFECTSTART DATE NOT NULL;
ALTER TABLE dbo.ERSSRESCFGTBL ALTER COLUMN DTMEFFECTEND DATE NOT NULL;
IF EXISTS
(
    SELECT 1 FROM dbo.ERSSRESCFGTBL AS a
    JOIN dbo.ERSSRESCFGTBL AS b ON b.UIDRESID = a.UIDRESID AND b.UIDRESCFGID <> a.UIDRESCFGID
       AND a.DTMEFFECTSTART <= COALESCE(b.DTMEFFECTEND, CONVERT(date, '99991231', 112))
       AND b.DTMEFFECTSTART <= COALESCE(a.DTMEFFECTEND, CONVERT(date, '99991231', 112))
       AND a.UIDRESCFGID < b.UIDRESCFGID
)
    THROW 50103, 'Resource configuration periods overlap after conversion. Resolve the version periods, then rerun.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ERSSRESCFGTBL') AND name = N'DF_ERSSRESCFGTBL_DTMEFFECTEND')
    ALTER TABLE dbo.ERSSRESCFGTBL ADD CONSTRAINT DF_ERSSRESCFGTBL_DTMEFFECTEND DEFAULT (CONVERT(date, '99991231', 112)) FOR DTMEFFECTEND;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ERSSRESCFGTBL') AND name = N'CK_ERSSRESCFGTBL_EFFECTIVE')
    ALTER TABLE dbo.ERSSRESCFGTBL ADD CONSTRAINT CK_ERSSRESCFGTBL_EFFECTIVE CHECK (DTMEFFECTEND >= DTMEFFECTSTART);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ERSSRESFEETBL') AND name = N'UQ_ERSSRESFEETBL_CFG_IDN')
    CREATE UNIQUE INDEX UQ_ERSSRESFEETBL_CFG_IDN ON dbo.ERSSRESFEETBL (UIDRESCFGID, UIDIDNTYPE);

COMMIT TRANSACTION;
GO

IF DATABASE_PRINCIPAL_ID(N'erss_app') IS NOT NULL
BEGIN
    GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.ERSSRESCFGTBL TO [erss_app];
    GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.ERSSRESFEETBL TO [erss_app];
END;
GO






