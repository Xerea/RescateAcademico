using Microsoft.EntityFrameworkCore;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Services
{
    public class StudentAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly CurrentUserContext _currentUser;

        public StudentAccessService(ApplicationDbContext context, CurrentUserContext currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Tutor?> GetCurrentTutorAsync()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId))
            {
                return null;
            }

            return await _context.Tutores.FirstOrDefaultAsync(t => t.UserId == _currentUser.UserId);
        }

        public async Task<Alumno?> GetCurrentAlumnoAsync()
        {
            if (string.IsNullOrEmpty(_currentUser.UserId))
            {
                return null;
            }

            return await _context.Alumnos.FirstOrDefaultAsync(a => a.UserId == _currentUser.UserId);
        }

        public IQueryable<Alumno> ApplyVisibleStudents(IQueryable<Alumno> query)
        {
            if (_currentUser.IsAdminOrAutoridad)
            {
                return query;
            }

            if (_currentUser.IsTutor && !string.IsNullOrEmpty(_currentUser.UserId))
            {
                return query.Where(a => _context.Grupos
                    .Any(g => g.Profesor != null && g.Profesor.UserId == _currentUser.UserId && a.GrupoId == g.Id));
            }

            if (_currentUser.IsAlumno && !string.IsNullOrEmpty(_currentUser.UserId))
            {
                return query.Where(a => a.UserId == _currentUser.UserId);
            }

            return query.Where(_ => false);
        }

        public async Task<bool> CanAccessAlumnoAsync(string matricula)
        {
            return await ApplyVisibleStudents(_context.Alumnos.AsQueryable())
                .AnyAsync(a => a.Matricula == matricula);
        }

        public async Task<List<string>> GetVisibleMatriculasAsync()
        {
            return await ApplyVisibleStudents(_context.Alumnos.AsQueryable())
                .Select(a => a.Matricula)
                .ToListAsync();
        }
    }
}
