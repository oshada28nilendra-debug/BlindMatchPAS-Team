using BlindMatchPAS.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ResearchArea> ResearchAreas { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<SupervisorExpertise> SupervisorExpertises { get; set; }
        public DbSet<Match> Matches { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Composite PK for SupervisorExpertise
            builder.Entity<SupervisorExpertise>()
                .HasKey(se => new { se.SupervisorId, se.ResearchAreaId });

            // SupervisorExpertise -> Supervisor
            builder.Entity<SupervisorExpertise>()
                .HasOne(se => se.Supervisor)
                .WithMany(u => u.Expertises)
                .HasForeignKey(se => se.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);

            // SupervisorExpertise -> ResearchArea
            builder.Entity<SupervisorExpertise>()
                .HasOne(se => se.ResearchArea)
                .WithMany(r => r.SupervisorExpertises)
                .HasForeignKey(se => se.ResearchAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project -> Student
            builder.Entity<Project>()
                .HasOne(p => p.Student)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project -> ResearchArea
            builder.Entity<Project>()
                .HasOne(p => p.ResearchArea)
                .WithMany(r => r.Projects)
                .HasForeignKey(p => p.ResearchAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match -> Project
            builder.Entity<Match>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Matches)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Match -> Supervisor
            builder.Entity<Match>()
                .HasOne(m => m.Supervisor)
                .WithMany(u => u.SupervisedMatches)
                .HasForeignKey(m => m.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: one supervisor can only express interest once per project
            builder.Entity<Match>()
                .HasIndex(m => new { m.ProjectId, m.SupervisorId })
                .IsUnique();

            // Indexes
            builder.Entity<Project>()
                .HasIndex(p => p.Status);

            builder.Entity<Project>()
                .HasIndex(p => p.StudentId);

            builder.Entity<Match>()
                .HasIndex(m => m.Status);
        }
    }
}
