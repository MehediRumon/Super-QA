using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;

namespace SuperQA.Infrastructure.Data;

public class SuperQADbContext : DbContext
{
    public SuperQADbContext(DbContextOptions<SuperQADbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<TestExecution> TestExecutions => Set<TestExecution>();
    public DbSet<DefectPrediction> DefectPredictions => Set<DefectPrediction>();
    public DbSet<AIPromptLog> AIPromptLogs => Set<AIPromptLog>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<ExtensionTestData> ExtensionTestData => Set<ExtensionTestData>();
    public DbSet<HealingHistory> HealingHistories => Set<HealingHistory>();
    public DbSet<CodeEditorScript> CodeEditorScripts => Set<CodeEditorScript>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Requirement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Type).HasMaxLength(50);
            
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Requirements)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.Project)
                .WithMany(p => p.TestCases)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Requirement)
                .WithMany(r => r.TestCases)
                .HasForeignKey(e => e.RequirementId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TestExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.TestCase)
                .WithMany(tc => tc.TestExecutions)
                .HasForeignKey(e => e.TestCaseId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Project)
                .WithMany(p => p.TestExecutions)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<DefectPrediction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Module).HasMaxLength(200);
            
            entity.HasOne(e => e.TestExecution)
                .WithMany(te => te.DefectPredictions)
                .HasForeignKey(e => e.TestExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIPromptLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PromptType).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
        });

        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OpenAIApiKey).HasMaxLength(500);
            entity.Property(e => e.SelectedModel).HasMaxLength(100);
        });

        modelBuilder.Entity<ExtensionTestData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TestName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ApplicationUrl).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.StepsJson).IsRequired();
            
            entity.HasOne(e => e.TestCase)
                .WithMany()
                .HasForeignKey(e => e.TestCaseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HealingHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HealingType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OldLocator).HasMaxLength(500);
            entity.Property(e => e.NewLocator).HasMaxLength(500);
            
            entity.HasOne(e => e.TestCase)
                .WithMany()
                .HasForeignKey(e => e.TestCaseId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.TestExecution)
                .WithMany()
                .HasForeignKey(e => e.TestExecutionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CodeEditorScript>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TestName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ApplicationUrl).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.GherkinSteps).IsRequired();
            entity.Property(e => e.GeneratedScript).IsRequired();
        });
    }
}
