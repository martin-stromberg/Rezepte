namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Class representing the repository paths.
/// </summary>
public static class RepositoryPaths
{
    /// <summary>
    /// Find repository root.
    /// </summary>
    /// <returns>The result.</returns>
    public static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Rezepte.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Read repository file.
    /// </summary>
    /// <param name="relativePathParts">The relative path parts parameter.</param>
    /// <returns>The result.</returns>
    public static string ReadRepositoryFile(params string[] relativePathParts)
    {
        var directory = FindRepositoryRoot();

        var candidate = Path.Combine(directory.FullName, Path.Combine(relativePathParts));
        if (File.Exists(candidate))
        {
            return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException(
            $"Could not locate repository file '{Path.Combine(relativePathParts)}' from '{directory.FullName}'.");
    }
}
