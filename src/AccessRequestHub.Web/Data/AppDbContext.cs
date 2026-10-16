using AccessRequestHub.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestHub.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<PolicyVersion> PolicyVersions => Set<PolicyVersion>();
        public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<User>(e =>
            {
                e.Property(x => x.Email).HasMaxLength(200).IsRequired();
                e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.Email).IsUnique();
                e.HasOne<User>().WithMany().HasForeignKey(x => x.ManagerId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            b.Entity<Application>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.Name).IsUnique();
                e.HasOne(x => x.SystemOwner).WithMany().HasForeignKey(x => x.SystemOwnerId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            b.Entity<PolicyVersion>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(20).IsRequired();
                e.HasIndex(x => x.Name).IsUnique();
            });

            b.Entity<AccessRequest>(e =>
            {
                e.Property(x => x.RowVersion).IsRowVersion();
                e.Property(x => x.Justification).HasMaxLength(1000).IsRequired();
                e.Property(x => x.RejectReason).HasMaxLength(1000);
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
                e.Property(x => x.Environment).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.AccessLevel).HasConversion<string>().HasMaxLength(20);

                // Idempotency dijaga database, bukan hanya kode aplikasi
                e.HasIndex(x => new { x.RequesterId, x.ClientRequestId }).IsUnique();
                // Untuk query inbox
                e.HasIndex(x => new { x.Status, x.ApplicationId });

                e.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId)
                    .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId)
                    .OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.PolicyVersion).WithMany().HasForeignKey(x => x.PolicyVersionId)
                    .OnDelete(DeleteBehavior.NoAction);
                e.HasMany(x => x.AuditEvents).WithOne().HasForeignKey(x => x.AccessRequestId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_AccessRequests_Status",
                        "[Status] IN ('PendingManagerApproval','PendingSystemOwnerApproval','Approved','Rejected')");
                    t.HasCheckConstraint("CK_AccessRequests_Environment",
                        "[Environment] IN ('NonProduction','Production')");
                    t.HasCheckConstraint("CK_AccessRequests_AccessLevel",
                        "[AccessLevel] IN ('Read','Admin')");
                    t.HasCheckConstraint("CK_AccessRequests_Justification",
                        "LEN(LTRIM(RTRIM([Justification]))) > 0");
                    t.HasCheckConstraint("CK_AccessRequests_RejectReason",
                        "[Status] <> 'Rejected' OR LEN(LTRIM(RTRIM(ISNULL([RejectReason],'')))) > 0");
                });
            });

            b.Entity<AuditEvent>(e =>
            {
                e.Property(x => x.EventType).HasMaxLength(50).IsRequired();
                e.Property(x => x.Reason).HasMaxLength(1000);
                e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(40);
                e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(40);
                e.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId)
                    .OnDelete(DeleteBehavior.NoAction);
                e.HasIndex(x => new { x.AccessRequestId, x.OccurredAtUtc });
            });

            Seed(b);
        }

        private static void Seed(ModelBuilder b)
        {
            b.Entity<User>().HasData(
                new User { Id = SeedIds.Bob, Email = "bob@example.local", DisplayName = "Bob" },
                new User { Id = SeedIds.Alice, Email = "alice@example.local", DisplayName = "Alice", ManagerId = SeedIds.Bob },
                new User { Id = SeedIds.Carol, Email = "carol@example.local", DisplayName = "Carol" },
                new User { Id = SeedIds.Dana, Email = "dana@example.local", DisplayName = "Dana" },
                new User { Id = SeedIds.Erin, Email = "erin@example.local", DisplayName = "Erin", IsAuditor = true });

            b.Entity<Application>().HasData(
                new Application { Id = SeedIds.Crm, Name = "CRM", SystemOwnerId = SeedIds.Carol },
                new Application { Id = SeedIds.FinancePortal, Name = "Finance Portal", SystemOwnerId = SeedIds.Dana });

            b.Entity<PolicyVersion>().HasData(
                new PolicyVersion { Id = SeedIds.PolicyV1, Name = "v1" });
        }
    }
}
