-- =============================================================================
-- Seed Data - Financial ChatBot Demo Data
-- =============================================================================

-- ─── Bank Accounts ────────────────────────────────────────────────────────────
INSERT INTO BankAccounts (AccountNumber, AccountHolderName, AccountType, Currency, Balance, OpenedDate, BranchCode)
VALUES
    ('ACC-001-2024', 'Acme Corporation',     'Business', 'USD', 485230.50, '2018-03-15', 'NYC-001'),
    ('ACC-002-2024', 'John M. Smith',        'Checking', 'USD',  12540.75, '2020-07-22', 'NYC-002'),
    ('ACC-003-2024', 'GlobalTech Holdings',  'Business', 'USD', 2340000.00,'2015-01-10', 'LON-001'),
    ('ACC-004-2024', 'Sarah J. Williams',    'Savings',  'USD',  89450.00, '2019-11-05', 'NYC-003');
GO

-- ─── Bank Statements (Last 3 months for Acme) ────────────────────────────────
INSERT INTO BankStatements (BankAccountId, StatementPeriod, StatementDate, PeriodStartDate, PeriodEndDate, OpeningBalance, ClosingBalance, TotalCredits, TotalDebits, TransactionCount)
VALUES
    (1, '2025-10', '2025-10-31', '2025-10-01', '2025-10-31', 410000.00, 498500.00, 250000.00, 161500.00, 42),
    (1, '2025-11', '2025-11-30', '2025-11-01', '2025-11-30', 498500.00, 462000.00, 180000.00, 216500.00, 38),
    (1, '2025-12', '2025-12-31', '2025-12-01', '2025-12-31', 462000.00, 485230.50, 195000.00, 171769.50, 45);
GO

-- ─── Sample Transactions (December 2025, Acme) ───────────────────────────────
INSERT INTO Transactions (BankStatementId, BankAccountId, TransactionReference, TransactionDate, ValueDate, Description, TransactionType, Category, Amount, RunningBalance, Channel, CounterpartyName)
VALUES
    (3, 1, 'TXN-202512-001', '2025-12-02', '2025-12-02', 'Client payment - Invoice INV-4521', 'Credit', 'Revenue',      85000.00, 547000.00, 'Online',    'TechSolutions Inc'),
    (3, 1, 'TXN-202512-002', '2025-12-03', '2025-12-03', 'Office rent - December',            'Debit',  'Rent',         22000.00, 525000.00, 'Online',    'Prime Real Estate'),
    (3, 1, 'TXN-202512-004', '2025-12-05', '2025-12-05', 'Payroll - December',                'Debit',  'Payroll',      95000.00, 430000.00, 'Online',    'Payroll Services'),
    (3, 1, 'TXN-202512-005', '2025-12-07', '2025-12-07', 'Client payment - Invoice INV-4522', 'Credit', 'Revenue',      55000.00, 485000.00, 'Online',    'InnovateCo LLC'),
    (3, 1, 'TXN-202512-006', '2025-12-10', '2025-12-10', 'AWS Cloud Services - December',     'Debit',  'Technology',    8500.00, 476500.00, 'Online',    'Amazon Web Services'),
    (3, 1, 'TXN-202512-007', '2025-12-12', '2025-12-12', 'Marketing campaign - Q4',           'Debit',  'Marketing',    15000.00, 461500.00, 'Online',    'Digital Marketing Co'),
    (3, 1, 'TXN-202512-008', '2025-12-15', '2025-12-15', 'Client retainer - monthly fee',    'Credit', 'Revenue',      35000.00, 496500.00, 'Online',    'Enterprise Corp'),
    (3, 1, 'TXN-202512-009', '2025-12-18', '2025-12-18', 'Office supplies',                   'Debit',  'Operations',    2100.00, 494400.00, 'Online',    'Office Depot'),
    (3, 1, 'TXN-202512-010', '2025-12-20', '2025-12-20', 'Insurance premium - Q4',            'Debit',  'Insurance',     9800.00, 484600.00, 'Online',    'BusinessShield Insurance'),
    (3, 1, 'TXN-202512-011', '2025-12-22', '2025-12-22', 'Client payment - Invoice INV-4525', 'Credit', 'Revenue',      20000.00, 504600.00, 'Online',    'StartUp Ventures'),
    (3, 1, 'TXN-202512-012', '2025-12-28', '2025-12-28', 'Utilities - December',              'Debit',  'Utilities',     4200.00, 500400.00, 'Branch',    'City Power'),
    (3, 1, 'TXN-202512-013', '2025-12-30', '2025-12-30', 'Year-end professional services',   'Debit',  'Professional',  15170.00, 485230.00, 'Online',    'Grant & Associates CPA');
GO

-- ─── Capital Structures ───────────────────────────────────────────────────────
INSERT INTO CapitalStructures (
    CompanyName, CompanyCode, FiscalYear, Quarter,
    CommonStock, PreferredStock, RetainedEarnings, AdditionalPaidInCapital, TreasuryStock, TotalEquity,
    ShortTermDebt, LongTermDebt, CurrentPortionLongTermDebt, Bonds, Debentures, TotalDebt,
    TotalCapital, DebtToEquityRatio, DebtRatio, EquityRatio,
    WeightedAverageCostOfCapital, CostOfDebt, CostOfEquity,
    MarketCapitalization, EnterpriseValue, SharesOutstanding, BookValuePerShare, MarketValuePerShare,
    ReportDate
)
VALUES
-- TechGiant Corp - FY2024 Annual
(
    'TechGiant Corporation', 'TGC', 2024, 'Annual',
    500.00, 50.00, 45000.00, 12000.00, -2500.00, 55050.00,
    3000.00, 25000.00, 1000.00, 10000.00, 0.00, 39000.00,
    94050.00, 0.7085, 0.4146, 0.5854,
    0.0892, 0.0425, 0.1250,
    185000.00, 218000.00, 10000.00, 5.505, 18.50,
    '2025-02-15'
),
-- TechGiant Corp - FY2023 Annual (historical)
(
    'TechGiant Corporation', 'TGC', 2023, 'Annual',
    500.00, 50.00, 38000.00, 12000.00, -2000.00, 48550.00,
    4000.00, 28000.00, 1200.00, 12000.00, 0.00, 45200.00,
    93750.00, 0.9310, 0.4822, 0.5178,
    0.0945, 0.0450, 0.1280,
    152000.00, 191000.00, 10000.00, 4.855, 15.20,
    '2024-02-14'
),
-- FinBank Ltd - FY2024 Annual
(
    'FinBank Limited', 'FBL', 2024, 'Annual',
    1000.00, 200.00, 8500.00, 3500.00, -500.00, 12700.00,
    15000.00, 85000.00, 5000.00, 40000.00, 10000.00, 155000.00,
    167700.00, 12.2047, 0.9243, 0.0757,
    0.0680, 0.0380, 0.1150,
    18500.00, 168000.00, 5000.00, 2.54, 3.70,
    '2025-03-10'
),
-- GreenEnergy Inc - FY2024 Annual
(
    'GreenEnergy Incorporated', 'GEI', 2024, 'Annual',
    300.00, 0.00, 2200.00, 5500.00, 0.00, 8000.00,
    500.00, 12000.00, 500.00, 5000.00, 2000.00, 20000.00,
    28000.00, 2.5000, 0.7143, 0.2857,
    0.0780, 0.0520, 0.1320,
    9500.00, 28500.00, 2000.00, 4.00, 4.75,
    '2025-02-28'
);
GO

-- ─── Capital Structure Notes ──────────────────────────────────────────────────
INSERT INTO CapitalStructureNotes (CapitalStructureId, NoteType, Title, Content)
VALUES
    (1, 'Debt',     'Senior Notes Maturity',    'USD 10B in 3.5% Senior Notes due March 2031. Investment grade rated BBB+ by S&P.'),
    (1, 'Equity',   'Share Buyback Program',    'Board approved USD 5B share repurchase program through December 2025.'),
    (1, 'Risk',     'Interest Rate Exposure',   'Approximately 65% of debt is fixed rate, limiting exposure to rate increases.'),
    (3, 'Covenant', 'Tier 1 Capital Ratio',     'Tier 1 Capital Ratio: 14.2%. Regulatory minimum is 8.0%. Strong buffer maintained.'),
    (3, 'Debt',     'Deposit Funding',           'Core deposits fund 72% of assets. Wholesale funding reliance reduced from 35% to 28%.'),
    (4, 'Debt',     'Green Bond Issuance',       'USD 2B Green Bond issued at 4.25% to fund renewable energy projects, ESG-aligned.');
GO

PRINT 'Seed data inserted successfully.';
GO
