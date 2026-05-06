namespace RescateAcademico.Services
{
    public class FileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        private static readonly Dictionary<string, string[]> AllowedMimeTypes = new()
        {
            [".pdf"] = new[] { "application/pdf" },
            [".doc"] = new[] { "application/msword" },
            [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            [".jpg"] = new[] { "image/jpeg" },
            [".jpeg"] = new[] { "image/jpeg" },
            [".png"] = new[] { "image/png" }
        };

        public FileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        public async Task<StoredFileResult> SavePostulacionDocumentAsync(IFormFile file)
        {
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                throw new InvalidOperationException("El archivo excede el tamano maximo permitido de 5 MB.");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedMimeTypes.ContainsKey(ext))
            {
                throw new InvalidOperationException("Tipo de archivo no permitido. Solo se aceptan PDF, DOC, DOCX, JPG, JPEG y PNG.");
            }

            if (!AllowedMimeTypes[ext].Contains(file.ContentType.ToLowerInvariant()))
            {
                throw new InvalidOperationException("El contenido del archivo no coincide con su extension.");
            }

            var safeFileName = $"{Guid.NewGuid()}{ext}";
            var storageRoot = GetStorageRoot();
            var uploadsFolder = Path.Combine(storageRoot, "postulaciones");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, safeFileName);
            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);

            return new StoredFileResult(file.FileName, safeFileName, filePath, file.Length);
        }

        private string GetStorageRoot()
        {
            var configured = _configuration["Uploads:Path"]
                ?? Environment.GetEnvironmentVariable("RAILWAY_VOLUME_MOUNT_PATH")
                ?? Environment.GetEnvironmentVariable("UPLOADS_PATH");

            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Path.Combine(configured, "uploads");
            }

            return Path.Combine(_environment.ContentRootPath, "App_Data", "uploads");
        }
    }

    public record StoredFileResult(string OriginalName, string StoredName, string Path, long Size);
}
