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
            if (!await HasExpectedFileSignatureAsync(file, ext))
            {
                throw new InvalidOperationException("El contenido del archivo no corresponde al tipo declarado.");
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

        public string? GetPostulacionDocumentPath(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;

            var folder = Path.GetFullPath(Path.Combine(GetStorageRoot(), "postulaciones"));
            var path = Path.GetFullPath(storedPath);
            var prefix = folder.EndsWith(Path.DirectorySeparatorChar)
                ? folder
                : folder + Path.DirectorySeparatorChar;

            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && File.Exists(path)
                ? path
                : null;
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

        private static async Task<bool> HasExpectedFileSignatureAsync(IFormFile file, string extension)
        {
            await using var stream = file.OpenReadStream();
            var header = new byte[8];
            var count = await stream.ReadAsync(header);
            if (count < 4) return false;

            return extension switch
            {
                ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => count >= 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".doc" => header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0,
                ".docx" => header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
                _ => false
            };
        }
    }

    public record StoredFileResult(string OriginalName, string StoredName, string Path, long Size);
}
