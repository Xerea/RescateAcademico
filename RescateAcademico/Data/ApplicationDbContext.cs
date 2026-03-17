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

        // Tablas Generales / Modelado de Datos
        public DbSet<Alumno> Alumnos { get; set; }
        public DbSet<Proyecto> Proyectos { get; set; }
        public DbSet<Postulacion> Postulaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
