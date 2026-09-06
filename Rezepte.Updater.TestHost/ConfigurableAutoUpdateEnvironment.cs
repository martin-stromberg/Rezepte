using msTools.Updater;

namespace Rezepte.Updater.TestHost;

/// <summary>
/// Custom <see cref="IAutoUpdateEnvironment"/> that allows the application directory to be configured
/// explicitly via settings. This prevents the test host from trying to update itself while it is running.
/// </summary>
public sealed class ConfigurableAutoUpdateEnvironment : IAutoUpdateEnvironment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurableAutoUpdateEnvironment"/> class.
    /// </summary>
    /// <param name="applicationDirectory">Directory the updater treats as the application installation folder.</param>
    public ConfigurableAutoUpdateEnvironment(string applicationDirectory)
    {
        ApplicationDirectory = applicationDirectory;
    }

    /// <summary>
    /// Gets the directory the updater treats as the application installation folder.
    /// </summary>
    public string ApplicationDirectory { get; }
}
