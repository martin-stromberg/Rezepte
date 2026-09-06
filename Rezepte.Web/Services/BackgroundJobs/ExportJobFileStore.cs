using Microsoft.AspNetCore.Hosting;

namespace Rezepte.Web.Services.BackgroundJobs;

/// <summary>
/// Represents the export job file store class.
/// </summary>
public sealed class ExportJobFileStore
{
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportJobFileStore"/> class.
    /// </summary>
    /// <param name="environment">The environment parameter.</param>
    public ExportJobFileStore(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string ExportsDirectory
    {
        get
        {
            var path = Path.Combine(_environment.ContentRootPath, "exports");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// Creates the safe file name.
    /// </summary>
    /// <param name="prefix">The prefix parameter.</param>
    /// <param name="userId">The user id parameter.</param>
    /// <param name="jobId">The job id parameter.</param>
    /// <returns>The result.</returns>
    public string CreateSafeFileName(string prefix, string userId, Guid jobId)
    {
        var input = $"{prefix}-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{jobId}.zip";
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }

        return input;
    }

    /// <summary>
    /// Gets the path for file name.
    /// </summary>
    /// <param name="fileName">The file name parameter.</param>
    /// <returns>The result.</returns>
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
