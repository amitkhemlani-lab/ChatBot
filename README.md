# Financial AI ChatBot

A .NET 8 AI-powered chatbot built on **Azure AI Foundry** that provides intelligent, conversational analysis of financial data stored in **Azure SQL**.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client (Web / API)                        │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTP/REST
┌────────────────────────────▼────────────────────────────────────┐
│               FinancialChatBot.API  (ASP.NET Core 8)            │
│  ┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │ ChatController│  │BankStmtController│  │CapStructController│ │
│  └──────┬───────┘  └────────┬─────────┘  └────────┬─────────┘  │
└─────────┼───────────────────┼─────────────────────┼────────────┘
          │                   │                     │
┌─────────▼───────────────────▼─────────────────────▼────────────┐
│             FinancialChatBot.Infrastructure                      │
│  ┌────────────────────┐   ┌──────────────────────────────────┐  │
│  │  FoundryAgentService│   │  EF Core Repositories            │  │
│  │  (Azure AI Foundry) │   │  BankStatement / CapStructure /  │  │
│  └────────┬───────────┘   │  Chat Repositories               │  │
│           │                └──────────────┬───────────────────┘  │
└───────────┼───────────────────────────────┼────────────────────┘
            │                               │
    ┌───────▼──────┐              ┌─────────▼─────────┐
    │ Azure AI      │              │  Azure SQL Database│
    │ Foundry       │              │  FinancialChatBotDb│
    │ (GPT-4o Agent)│              └───────────────────┘
    └───────────────┘
```

### Projects

| Project | Description |
|---------|-------------|
| `FinancialChatBot.Core` | Domain entities, interfaces, DTOs, models |
| `FinancialChatBot.Infrastructure` | EF Core DbContext, repositories, Foundry agent service |
| `FinancialChatBot.API` | ASP.NET Core Web API with Swagger UI |

---

## Financial Data Domain

### Bank Statement Module
- **BankAccounts** — Account master data (number, holder, type, balance)
- **BankStatements** — Monthly statements with period totals
- **Transactions** — Individual debit/credit transactions with categories

### Capital Structure Module
- **CapitalStructures** — Equity/debt breakdown by company, year, and quarter
- **CapitalStructureNotes** — Analyst notes (covenants, risk, ESG)

### Chat Module
- **ChatSessions** — Conversation threads (linked to Foundry threads)
- **ChatMessages** — Full message history with SQL queries and token usage

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Azure subscription with:
  - **Azure AI Foundry** project with a GPT-4o deployment
  - **Azure SQL** database
  - **Azure Active Directory** (for `DefaultAzureCredential`)

---

## Quick Start

### 1. Clone & Configure

```bash
git clone <repo-url>
cd ChatBot
```

Update `src/FinancialChatBot.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "AzureSql": "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=FinancialChatBotDb;..."
  },
  "AzureAIFoundry": {
    "ConnectionString": "<your-foundry-project-connection-string>",
    "ModelDeploymentName": "gpt-4o"
  }
}
```

### 2. Create the Azure SQL Database

```sql
-- Run against your Azure SQL server
CREATE DATABASE FinancialChatBotDb;
```

Then execute the schema and seed scripts:

```bash
sqlcmd -S <server>.database.windows.net -d FinancialChatBotDb \
       -G -i scripts/01_CreateSchema.sql

sqlcmd -S <server>.database.windows.net -d FinancialChatBotDb \
       -G -i scripts/02_SeedData.sql
```

### 3. Run the API

```bash
cd src/FinancialChatBot.API
dotnet restore
dotnet run
```

The Swagger UI will open at `http://localhost:5000`.

---

## API Endpoints

### Chat

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/chat/message` | Send a message to the AI assistant |
| `POST` | `/api/chat/sessions` | Create a new chat session |
| `GET`  | `/api/chat/sessions?userId={id}` | List sessions for a user |
| `GET`  | `/api/chat/sessions/{id}/history` | Get full message history |

### Bank Statements

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/bankstatement/accounts` | List all accounts |
| `GET` | `/api/bankstatement/accounts/{num}` | Get account details |
| `GET` | `/api/bankstatement/accounts/{num}/statements` | Get statements (with date filter) |
| `GET` | `/api/bankstatement/accounts/{num}/transactions` | Get transactions |
| `GET` | `/api/bankstatement/accounts/{num}/spending-summary` | Spending by category |

### Capital Structure

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/capitalstructure` | All companies (latest) |
| `GET` | `/api/capitalstructure/{code}/latest` | Latest for one company |
| `GET` | `/api/capitalstructure/{code}/{year}` | By fiscal year |
| `GET` | `/api/capitalstructure/{code}/trend?years=5` | Historical trend |

---

## Example Chat Queries

```
"What was Acme Corporation's total spending in December 2025?"

"Compare TechGiant and FinBank's debt-to-equity ratios."

"Show me the top spending categories for account ACC-001-2024 in Q4 2025."

"What is TechGiant's WACC and how does it compare to its cost of equity?"

"Identify any unusual transactions in Acme's December bank statement."

"Has TechGiant's leverage improved over the past 2 years?"
```

---

## Azure AI Foundry Setup

1. Create an **Azure AI Foundry** project in the Azure portal.
2. Deploy **GPT-4o** as a model endpoint.
3. Copy the **project connection string** from the Foundry portal.
4. Grant your app's managed identity the `Azure AI Developer` role on the Foundry project.
5. Set `AzureAIFoundry:ConnectionString` in configuration.

> The chatbot uses `DefaultAzureCredential` — in local dev, `az login` works; in Azure, use Managed Identity.

---

## Security Notes

- Secrets are managed via **Azure Key Vault** or **ASP.NET User Secrets** (local dev only)
- SQL queries generated by the AI are executed as **read-only SELECT** statements only
- All connections use **Azure Active Directory authentication** (no passwords in connection strings)
- CORS is restricted to configured allowed origins

---

## Project Structure

```
ChatBot/
├── FinancialChatBot.sln
├── src/
│   ├── FinancialChatBot.Core/
│   │   ├── Entities/          # Domain models
│   │   ├── Interfaces/        # Repository + service contracts
│   │   ├── Models/            # Chat request/response models
│   │   └── DTOs/              # Data transfer objects
│   ├── FinancialChatBot.Infrastructure/
│   │   ├── Data/              # EF Core DbContext + migrations
│   │   ├── Repositories/      # EF Core implementations
│   │   ├── Services/          # FoundryAgentService (AI integration)
│   │   └── Extensions/        # DI registration
│   └── FinancialChatBot.API/
│       ├── Controllers/       # REST API controllers
│       ├── Middleware/        # Exception handling
│       ├── Models/            # API request/response models
│       ├── Program.cs         # App entry point + DI
│       └── appsettings.json   # Configuration
└── scripts/
    ├── 01_CreateSchema.sql    # Azure SQL DDL
    └── 02_SeedData.sql        # Demo data
```
