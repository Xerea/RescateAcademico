using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RescateAcademico.Models;

namespace RescateAcademico.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // HU-RA-06, HU-RA-10, HU-RA-12
        public DbSet<Student> Students { get; set; }

        // HU-RA-06, HU-RA-11, HU-RA-12
        public DbSet<Grade> Grades { get; set; }

        // HU-RA-10
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<TutorAssignment> TutorAssignments { get; set; }

        // HU-RA-07, HU-RA-08, HU-RA-17
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectApplication> ProjectApplications { get; set; }

        // HU-RA-09
        public DbSet<Notification> Notifications { get; set; }

        // HU-RA-13
        public DbSet<SchoolCycle> SchoolCycles { get; set; }

        // HU-RA-14
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // HU-RA-14: Configurar tabla de auditoría como inmutable
            builder.Entity<AuditLog>().ToTable("AuditLogs");

            // Configurar relaciones y restricciones de eliminar en cascada
            builder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TutorAssignment>()
                .HasOne(ta => ta.Tutor)
                .WithMany(t => t.Assignments)
                .HasForeignKey(ta => ta.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TutorAssignment>()
                .HasOne(ta => ta.Student)
                .WithMany()
                .HasForeignKey(ta => ta.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectApplication>()
                .HasOne(pa => pa.Student)
                .WithMany(s => s.ProjectApplications)
                .HasForeignKey(pa => pa.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectApplication>()
                .HasOne(pa => pa.Project)
                .WithMany(p => p.Applications)
                .HasForeignKey(pa => pa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
