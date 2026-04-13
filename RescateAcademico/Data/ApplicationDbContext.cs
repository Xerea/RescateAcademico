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

        public DbSet<Alumno> Alumnos { get; set; }
        public DbSet<Proyecto> Proyectos { get; set; }
        public DbSet<Postulacion> Postulaciones { get; set; }
        public DbSet<Materia> Materias { get; set; }
        public DbSet<Calificacion> Calificaciones { get; set; }
        public DbSet<Tutor> Tutores { get; set; }
        public DbSet<AsignacionTutor> AsignacionesTutor { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<BitacoraLog> BitacoraLogs { get; set; }
        public DbSet<CicloEscolar> CiclosEscolares { get; set; }
        public DbSet<Carrera> Carreras { get; set; }
        public DbSet<Documento> Documentos { get; set; }
        public DbSet<Convocatoria> Convocatorias { get; set; }
        public DbSet<Estadistica> Estadisticas { get; set; }
        public DbSet<SugerenciaIA> SugerenciasIA { get; set; }
        public DbSet<PrediccionDesercion> PrediccionesDesercion { get; set; }
        public DbSet<Autenticacion> Autenticaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Alumno>()
                .HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Tutor>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Calificacion>()
                .HasOne(c => c.Alumno)
                .WithMany(a => a.Calificaciones)
                .HasForeignKey(c => c.AlumnoMatricula)
                .HasPrincipalKey(a => a.Matricula)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Calificacion>()
                .HasOne(c => c.Materia)
                .WithMany(m => m.Calificaciones)
                .HasForeignKey(c => c.MateriaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AsignacionTutor>()
                .HasOne(at => at.Tutor)
                .WithMany(t => t.AsignacionesTutor)
                .HasForeignKey(at => at.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AsignacionTutor>()
                .HasOne(at => at.Alumno)
                .WithMany()
                .HasForeignKey(at => at.AlumnoMatricula)
                .HasPrincipalKey(a => a.Matricula)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Postulacion>()
                .HasOne(p => p.Alumno)
                .WithMany()
                .HasForeignKey(p => p.AlumnoId)
                .HasPrincipalKey(a => a.Matricula)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Postulacion>()
                .HasOne(p => p.Proyecto)
                .WithMany()
                .HasForeignKey(p => p.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Convocatoria>()
                .HasOne(c => c.Proyecto)
                .WithMany()
                .HasForeignKey(c => c.ProyectoId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Documento>()
                .HasOne(d => d.Postulacion)
                .WithMany()
                .HasForeignKey(d => d.PostulacionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
