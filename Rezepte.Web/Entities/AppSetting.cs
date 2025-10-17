namespace Rezepte.Web.Entities;

public class AppSetting
{
    public string Key { get; set; } = string.Empty; // e.g. "AiEnabled"
    public string Value { get; set; } = string.Empty;
}