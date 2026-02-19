using FinancialChatBot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialChatBot.Infrastructure.Data;

public class FinancialDbContext : DbContext
{
    public FinancialDbContext(DbContextOptions<FinancialDbContext> options) : base(options) { }

    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CapitalStructure> CapitalStructures => Set<CapitalStructure>();
    public DbSet<CapitalStructureNote> CapitalStructureNotes => Set<CapitalStructureNote>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BankAccount
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.AccountNumber).IsUnique();
            entity.Property(e => e.AccountHolderName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AccountType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BranchCode).HasMaxLength(20);
        });

        // BankStatement
        modelBuilder.Entity<BankStatement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StatementPeriod).IsRequired().HasMaxLength(10);
            entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ClosingBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCredits).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDebits).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Account)
                  .WithMany(a => a.Statements)
                  .HasForeignKey(e => e.BankAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.BankAccountId, e.StatementPeriod }).IsUnique();
        });

        // Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionReference).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TransactionReference).IsUnique();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RunningBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CounterpartyName).HasMaxLength(200);
            entity.Property(e => e.CounterpartyAccount).HasMaxLength(30);
            entity.HasOne(e => e.Statement)
                  .WithMany(s => s.Transactions)
                  .HasForeignKey(e => e.BankStatementId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Account)
                  .WithMany()
                  .HasForeignKey(e => e.BankAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CapitalStructure
        modelBuilder.Entity<CapitalStructure>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CompanyCode).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => new { e.CompanyCode, e.FiscalYear, e.Quarter }).IsUnique();
            entity.Property(e => e.Quarter).IsRequired().HasMaxLength(10);
            entity.Property(e => e.CommonStock).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PreferredStock).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RetainedEarnings).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AdditionalPaidInCapital).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TreasuryStock).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalEquity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ShortTermDebt).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LongTermDebt).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CurrentPortionLongTermDebt).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Bonds).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Debentures).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDebt).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCapital).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DebtToEquityRatio).HasColumnType("decimal(10,4)");
            entity.Property(e => e.DebtRatio).HasColumnType("decimal(10,4)");
            entity.Property(e => e.EquityRatio).HasColumnType("decimal(10,4)");
            entity.Property(e => e.WeightedAverageCostOfCapital).HasColumnType("decimal(10,4)");
            entity.Property(e => e.CostOfDebt).HasColumnType("decimal(10,4)");
            entity.Property(e => e.CostOfEquity).HasColumnType("decimal(10,4)");
            entity.Property(e => e.MarketCapitalization).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EnterpriseValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SharesOutstanding).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BookValuePerShare).HasColumnType("decimal(10,4)");
            entity.Property(e => e.MarketValuePerShare).HasColumnType("decimal(10,4)");
        });

        // CapitalStructureNote
        modelBuilder.Entity<CapitalStructureNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NoteType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.HasOne(e => e.CapitalStructure)
                  .WithMany(c => c.Notes)
                  .HasForeignKey(e => e.CapitalStructureId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatSession
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FoundryThreadId).HasMaxLength(200);
            entity.HasIndex(e => e.UserId);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.FoundryMessageId).HasMaxLength(200);
            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Messages)
                  .HasForeignKey(e => e.ChatSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
