using msTools.Updater;

namespace Rezepte.Updater.TestHost;

/// <summary>
/// Custom <see cref="IAutoUpdateEnvironment"/> that allows the application directory to be configured
/// explicitly via settings. This prevents the test host from trying to update itself while it is running.
/// </summary>
public sealed class ConfigurableAutoUpdateEnvironment : IAutoUpdateEnvironment
{
    public ConfigurableAutoUpdateEnvironment(string applicationDirectory)
    {
        ApplicationDirectory = applicationDirectory;
    }

    public string ApplicationDirectory { get; }
}
