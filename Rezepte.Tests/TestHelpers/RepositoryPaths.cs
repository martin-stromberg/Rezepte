namespace Rezepte.Tests.TestHelpers;

public static class RepositoryPaths
{
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
