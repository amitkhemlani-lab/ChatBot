-- =============================================================================
-- Financial ChatBot - Azure SQL Schema
-- Database: FinancialChatBotDb
-- =============================================================================

-- Bank Accounts
CREATE TABLE BankAccounts (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    AccountNumber     NVARCHAR(20)    NOT NULL,
    AccountHolderName NVARCHAR(200)   NOT NULL,
    AccountType       NVARCHAR(50)    NOT NULL,  -- Checking, Savings, Business
    Currency          NVARCHAR(3)     NOT NULL DEFAULT 'USD',
    Balance           DECIMAL(18,2)   NOT NULL DEFAULT 0,
    OpenedDate        DATETIME2       NOT NULL,
    IsActive          BIT             NOT NULL DEFAULT 1,
    BranchCode        NVARCHAR(20)    NULL,
    CreatedAt         DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt         DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_BankAccounts_AccountNumber UNIQUE (AccountNumber)
);
CREATE INDEX IX_BankAccounts_IsActive ON BankAccounts(IsActive);

-- Bank Statements
CREATE TABLE BankStatements (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    BankAccountId    INT             NOT NULL,
    StatementPeriod  NVARCHAR(10)    NOT NULL,  -- YYYY-MM
    StatementDate    DATETIME2       NOT NULL,
    PeriodStartDate  DATETIME2       NOT NULL,
    PeriodEndDate    DATETIME2       NOT NULL,
    OpeningBalance   DECIMAL(18,2)   NOT NULL,
    ClosingBalance   DECIMAL(18,2)   NOT NULL,
    TotalCredits     DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TotalDebits      DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TransactionCount INT             NOT NULL DEFAULT 0,
    GeneratedAt      DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_BankStatements_BankAccounts FOREIGN KEY (BankAccountId)
        REFERENCES BankAccounts(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_BankStatements_AccountPeriod UNIQUE (BankAccountId, StatementPeriod)
);
CREATE INDEX IX_BankStatements_BankAccountId ON BankStatements(BankAccountId);
CREATE INDEX IX_BankStatements_Period ON BankStatements(StatementPeriod);

-- Transactions
CREATE TABLE Transactions (
    Id                   INT IDENTITY(1,1) PRIMARY KEY,
    BankStatementId      INT             NOT NULL,
    BankAccountId        INT             NOT NULL,
    TransactionReference NVARCHAR(50)    NOT NULL,
    TransactionDate      DATETIME2       NOT NULL,
    ValueDate            DATETIME2       NOT NULL,
    Description          NVARCHAR(500)   NOT NULL,
    TransactionType      NVARCHAR(20)    NOT NULL,  -- Credit, Debit
    Category             NVARCHAR(100)   NOT NULL,  -- Salary, Transfer, Payment, etc.
    Amount               DECIMAL(18,2)   NOT NULL,
    RunningBalance       DECIMAL(18,2)   NOT NULL,
    Narration            NVARCHAR(MAX)   NULL,
    CounterpartyName     NVARCHAR(200)   NULL,
    CounterpartyAccount  NVARCHAR(30)    NULL,
    Channel              NVARCHAR(50)    NOT NULL,  -- ATM, Online, Branch, POS
    Status               NVARCHAR(20)    NOT NULL DEFAULT 'Completed',
    CreatedAt            DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Transactions_BankStatements FOREIGN KEY (BankStatementId)
        REFERENCES BankStatements(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Transactions_BankAccounts FOREIGN KEY (BankAccountId)
        REFERENCES BankAccounts(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Transactions_Reference UNIQUE (TransactionReference)
);
CREATE INDEX IX_Transactions_BankAccountId_Date ON Transactions(BankAccountId, TransactionDate DESC);
CREATE INDEX IX_Transactions_Category ON Transactions(Category);
CREATE INDEX IX_Transactions_TransactionType ON Transactions(TransactionType);
CREATE INDEX IX_Transactions_BankStatementId ON Transactions(BankStatementId);

-- Capital Structures
CREATE TABLE CapitalStructures (
    Id                           INT IDENTITY(1,1) PRIMARY KEY,
    CompanyName                  NVARCHAR(200)   NOT NULL,
    CompanyCode                  NVARCHAR(20)    NOT NULL,
    FiscalYear                   INT             NOT NULL,
    Quarter                      NVARCHAR(10)    NOT NULL,  -- Q1, Q2, Q3, Q4, Annual
    -- Equity
    CommonStock                  DECIMAL(18,2)   NOT NULL DEFAULT 0,
    PreferredStock               DECIMAL(18,2)   NOT NULL DEFAULT 0,
    RetainedEarnings             DECIMAL(18,2)   NOT NULL DEFAULT 0,
    AdditionalPaidInCapital      DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TreasuryStock                DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TotalEquity                  DECIMAL(18,2)   NOT NULL DEFAULT 0,
    -- Debt
    ShortTermDebt                DECIMAL(18,2)   NOT NULL DEFAULT 0,
    LongTermDebt                 DECIMAL(18,2)   NOT NULL DEFAULT 0,
    CurrentPortionLongTermDebt   DECIMAL(18,2)   NOT NULL DEFAULT 0,
    Bonds                        DECIMAL(18,2)   NOT NULL DEFAULT 0,
    Debentures                   DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TotalDebt                    DECIMAL(18,2)   NOT NULL DEFAULT 0,
    -- Metrics
    TotalCapital                 DECIMAL(18,2)   NOT NULL DEFAULT 0,
    DebtToEquityRatio            DECIMAL(10,4)   NOT NULL DEFAULT 0,
    DebtRatio                    DECIMAL(10,4)   NOT NULL DEFAULT 0,
    EquityRatio                  DECIMAL(10,4)   NOT NULL DEFAULT 0,
    WeightedAverageCostOfCapital DECIMAL(10,4)   NOT NULL DEFAULT 0,
    CostOfDebt                   DECIMAL(10,4)   NOT NULL DEFAULT 0,
    CostOfEquity                 DECIMAL(10,4)   NOT NULL DEFAULT 0,
    -- Market
    MarketCapitalization         DECIMAL(18,2)   NOT NULL DEFAULT 0,
    EnterpriseValue              DECIMAL(18,2)   NOT NULL DEFAULT 0,
    SharesOutstanding            DECIMAL(18,2)   NOT NULL DEFAULT 0,
    BookValuePerShare            DECIMAL(10,4)   NOT NULL DEFAULT 0,
    MarketValuePerShare          DECIMAL(10,4)   NOT NULL DEFAULT 0,
    ReportDate                   DATETIME2       NOT NULL,
    CreatedAt                    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt                    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_CapitalStructures_CompanyPeriod UNIQUE (CompanyCode, FiscalYear, Quarter)
);
CREATE INDEX IX_CapitalStructures_CompanyCode ON CapitalStructures(CompanyCode);
CREATE INDEX IX_CapitalStructures_FiscalYear ON CapitalStructures(FiscalYear);

-- Capital Structure Notes
CREATE TABLE CapitalStructureNotes (
    Id                 INT IDENTITY(1,1) PRIMARY KEY,
    CapitalStructureId INT             NOT NULL,
    NoteType           NVARCHAR(50)    NOT NULL,  -- Debt, Equity, Risk, Covenant
    Title              NVARCHAR(200)   NOT NULL,
    Content            NVARCHAR(MAX)   NOT NULL,
    CreatedAt          DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_CapitalStructureNotes_CapitalStructures FOREIGN KEY (CapitalStructureId)
        REFERENCES CapitalStructures(Id) ON DELETE CASCADE
);

-- Chat Sessions
CREATE TABLE ChatSessions (
    Id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId           NVARCHAR(200)   NOT NULL,
    Title            NVARCHAR(500)   NOT NULL,
    FoundryThreadId  NVARCHAR(200)   NULL,
    CreatedAt        DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    LastActivityAt   DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    IsActive         BIT             NOT NULL DEFAULT 1
);
CREATE INDEX IX_ChatSessions_UserId ON ChatSessions(UserId);

-- Chat Messages
CREATE TABLE ChatMessages (
    Id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ChatSessionId    UNIQUEIDENTIFIER NOT NULL,
    Role             NVARCHAR(20)    NOT NULL,  -- user, assistant, system
    Content          NVARCHAR(MAX)   NOT NULL,
    FoundryMessageId NVARCHAR(200)   NULL,
    TokensUsed       INT             NULL,
    CreatedAt        DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    SqlQueryExecuted NVARCHAR(MAX)   NULL,
    DataContext      NVARCHAR(MAX)   NULL,
    CONSTRAINT FK_ChatMessages_ChatSessions FOREIGN KEY (ChatSessionId)
        REFERENCES ChatSessions(Id) ON DELETE CASCADE
);
CREATE INDEX IX_ChatMessages_ChatSessionId ON ChatMessages(ChatSessionId);

PRINT 'Schema created successfully.';
GO
