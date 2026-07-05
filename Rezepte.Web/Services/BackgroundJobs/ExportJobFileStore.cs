using Microsoft.AspNetCore.Hosting;

namespace Rezepte.Web.Services.BackgroundJobs;

public sealed class ExportJobFileStore
{
    private readonly IWebHostEnvironment _environment;

    public ExportJobFileStore(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string ExportsDirectory
    {
        get
        {
            var path = Path.Combine(_environment.ContentRootPath, "exports");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public string CreateSafeFileName(string prefix, string userId, Guid jobId)
    {
        var input = $"{prefix}-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{jobId}.zip";
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }

        return input;
    }

    public string GetPathForFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
        {
            throw new InvalidOperationException("Invalid export file name.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(ExportsDirectory, fileName));
        var rootPath = Path.GetFullPath(ExportsDirectory);
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid export file path.");
        }

        return fullPath;
    }
}
