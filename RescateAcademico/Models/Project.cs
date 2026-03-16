namespace RescateAcademico.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ICollection<ProjectApplication> Applications { get; set; } = new List<ProjectApplication>();
    }

    public class ProjectApplication
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int StudentId { get; set; }

        public Project? Project { get; set; }
        public Student? Student { get; set; }
    }

    public class Student
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();
    }

    public class Grade
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public decimal Value { get; set; }

        // Navigation
        public Student? Student { get; set; }
    }

    public class Tutor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<TutorAssignment> Assignments { get; set; } = new List<TutorAssignment>();
    }

    public class TutorAssignment
    {
        public int Id { get; set; }
        public int TutorId { get; set; }
        public int StudentId { get; set; }

        public Tutor? Tutor { get; set; }
        public Student? Student { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }
    }

    public class SchoolCycle
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
    }
}
