namespace Rezepte.Web.Configuration;

public sealed class AIOptions
{
    public bool Simulate { get; set; } = false;
    public bool EnableCache { get; set; } = false;
    public int CacheDurationHours { get; set; } = 24;
}