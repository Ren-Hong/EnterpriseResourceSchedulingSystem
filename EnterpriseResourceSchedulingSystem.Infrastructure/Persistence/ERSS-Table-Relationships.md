# ERSS 資料表與關聯

此圖依 `InitializeErss.sql` 與 `UpgradeSchedulingSchema.sql` 執行後的 ERSS schema 整理。

```mermaid
erDiagram
    ERSSACCTBL {
        uniqueidentifier UIDACCTID PK
        nvarchar NVCHUSERNAME UK
        varchar VCHPASSWORDHASH
        nvarchar NVCHLEVEL
    }
    ERSSUSERTBL {
        uniqueidentifier UIDUSERID PK
        uniqueidentifier UIDIDNTYPE FK
        nvarchar NVCHDISPLAYNAME
    }
    ERSSIDNTBL {
        uniqueidentifier UIDIDNTYPE PK
        nvarchar NVCHIDENTITYNAME UK
    }
    ERSSRESTBL {
        uniqueidentifier UIDRESID PK
        nvarchar NVCHRESNAME
    }
    ERSSRESCFGTBL {
        uniqueidentifier UIDRESCFGID PK
        uniqueidentifier UIDRESID FK
        int NUMRESCFGNO
        nvarchar NVCHRESNAME
        int INTMAXUSER
        date DTMEFFECTSTART
        date DTMEFFECTEND
    }
    ERSSRESFEETBL {
        uniqueidentifier UIDRESFEEID PK
        uniqueidentifier UIDRESCFGID FK
        uniqueidentifier UIDIDNTYPE FK
        decimal NUMFEE
    }
    ERSSUSERRES {
        uniqueidentifier UIDUSERID PK, FK
        uniqueidentifier UIDRESID PK, FK
        date DTMEFFECTSTART
        date DTMEFFECTEND
    }
    ERSSSLOTTBL {
        uniqueidentifier UIDSLOTID PK
        uniqueidentifier UIDRESID FK
        uniqueidentifier UIDRESCFGID FK
        datetime2 DTMSTARTAT
        datetime2 DTMENDAT
        int INTCAPACITY
    }
    ERSSSLOTFEETBL {
        uniqueidentifier UIDSLOTFEEID PK
        uniqueidentifier UIDSLOTID FK
        uniqueidentifier UIDIDNTYPE FK
        decimal NUMFEE
    }
    ERSSSCHTBL {
        uniqueidentifier UIDSCHID PK
        uniqueidentifier UIDSLOTID FK
        uniqueidentifier UIDUSERID FK
        uniqueidentifier UIDIDNTYPE FK
        uniqueidentifier UIDSLOTFEEID FK
        decimal NUMFEE
    }

    ERSSIDNTBL ||--o{ ERSSUSERTBL : "一種身分可屬於多人；人員身分可空值"
    ERSSRESTBL ||--o{ ERSSRESCFGTBL : "資源版本"
    ERSSRESCFGTBL ||--o{ ERSSRESFEETBL : "版本費率"
    ERSSIDNTBL ||--o{ ERSSRESFEETBL : "身分別費率"
    ERSSUSERTBL ||--o{ ERSSUSERRES : "人員資源權限"
    ERSSRESTBL ||--o{ ERSSUSERRES : "可用資源"
    ERSSRESTBL ||--o{ ERSSSLOTTBL : "資源時段"
    ERSSRESCFGTBL ||--o{ ERSSSLOTTBL : "時段採用的資源版本"
    ERSSSLOTTBL ||--o{ ERSSSLOTFEETBL : "時段費用快照"
    ERSSIDNTBL ||--o{ ERSSSLOTFEETBL : "適用身分"
    ERSSSLOTTBL ||--o{ ERSSSCHTBL : "一個時段多筆實際預約"
    ERSSUSERTBL ||--o{ ERSSSCHTBL : "人員預約"
    ERSSIDNTBL ||--o{ ERSSSCHTBL : "預約時身分快照"
    ERSSSLOTFEETBL o|--o{ ERSSSCHTBL : "選用時段費率；預約另保存 NUMFEE"
```

## 關聯摘要

- `ERSSACCTBL` 是登入帳戶；它刻意不連到 `ERSSUSERTBL`。人員名單不需要帳密。
- `ERSSUSERTBL.UIDIDNTYPE` 可為 `NULL`；有身分別時參照 `ERSSIDNTBL`。
- `ERSSRESTBL` 是穩定資源主檔；`ERSSRESCFGTBL` 保存每次設定版本與生效區間。
- `ERSSRESCFGTBL` 保存設定版本的生效起訖（`DATE`），結束日為 `NULL` 代表持續有效；新增版本會結束前一版本並複製該版本費率。`ERSSRESFEETBL` 每個版本每種身分別一筆，只保存版本、身分別及費用。
- `ERSSUSERRES` 是人員與資源的多對多使用權限，複合主鍵為 `(UIDUSERID, UIDRESID)`。
- `ERSSSLOTTBL` 是可排時段，不是實際預約；容量可低於設定版本的資源人數上限。
- `ERSSSLOTFEETBL` 在時段建立時保存各身分別的費率快照。
- `ERSSSCHTBL` 是實際預約，一人一筆；每筆保留預約身分及 `NUMFEE` 金額快照。

## 舊表遷移

- 舊 `ERSSSCHTBL`（可排時段）更名為 `ERSSSLOTTBL`。
- 舊 `ERSSSCHUSERTBL`（時段人員明細）更名為 `ERSSSCHTBL`。
- 既有資源初始化為設定版本 1；既有費率指向版本 1；既有時段亦指向版本 1。
- 既有預約保留 `NUMFEE`，不會自動填 `UIDSLOTFEEID`，避免把歷史預約錯配到新的費率快照。
- `UpgradeResourceFeeVersioning.sql` 會將舊費率的共同生效期間移到資源版本，並移除費率列日期。若同一版本內各身分期間不一致或同一身分有多筆費率，遷移會停止並保留原資料，需先拆成多個設定版本。
