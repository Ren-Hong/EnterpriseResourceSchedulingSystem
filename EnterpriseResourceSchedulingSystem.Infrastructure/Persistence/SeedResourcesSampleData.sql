USE [ERSS];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Resources TABLE
(
    NVCHRESNAME NVARCHAR(100) NOT NULL,
    NVCHRESDESC NVARCHAR(500) NULL,
    INTMAXUSER INT NOT NULL
);

INSERT INTO @Resources (NVCHRESNAME, NVCHRESDESC, INTMAXUSER)
VALUES
    (N'會議室 A', N'適合部門會議與小型討論，配備視訊會議設備。', 12),
    (N'會議室 B', N'小型會議空間，適合專案討論與面談。', 8),
    (N'教育訓練教室', N'可供內部教育訓練與課程使用，配備投影設備。', 30),
    (N'多功能活動廳', N'適合大型簡報、員工活動與跨部門會議。', 60),
    (N'公務車 A', N'五人座公務車，供公務交通使用。', 5),
    (N'公務車 B', N'七人座公務車，供多人公務交通使用。', 7),
    (N'簡報投影設備', N'可移動式投影設備，預約期間由一組人員使用。', 1),
    (N'攝影設備組', N'包含攝影機與基本收音設備，供內部拍攝使用。', 3);

INSERT INTO dbo.ERSSRESTBL (NVCHRESNAME, NVCHRESDESC, INTMAXUSER, VCHCREATEBY)
SELECT source.NVCHRESNAME, source.NVCHRESDESC, source.INTMAXUSER, N'DEMO'
FROM @Resources AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.ERSSRESTBL AS existing
    WHERE existing.NVCHRESNAME = source.NVCHRESNAME
);

COMMIT TRANSACTION;
GO
