using CertEmpire.Models.CommonModel;
using CertEmpire.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CertEmpire.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
        public DbSet<Question> Questions { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ReviewTask> ReviewTasks { get; set; }
        public DbSet<TaskVote> TaskVotes { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<UserFilePrice> UserFilePrices { get; set; }
        public DbSet<Reward> Rewards {  get; set; }
        public DbSet<Withdrawal> Withdrawals {  get; set; }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ChangeTracker.HasChanges())
            {
                foreach (var entry in ChangeTracker.Entries<AuditableBaseEntity>())
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.Entity.Created = DateTime.UtcNow;
                            break;
                        case EntityState.Modified:
                            entry.Entity.LastModified = DateTime.UtcNow;
                            break;
                    }
                }
            }
            return await base.SaveChangesAsync(cancellationToken);
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            //All Decimals will have 18,6 Range
            foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(6);
            }
            builder.Entity<ReviewTask>()
       .HasOne(rt => rt.Report)
       .WithMany(r => r.ReviewTasks)
       .HasForeignKey(rt => rt.ReportId);
            base.OnModelCreating(builder);
        }
    }
}