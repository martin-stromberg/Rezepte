namespace Rezepte.Web.Entities;

public class RecipeImage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string RecipeId { get; set; } = string.Empty;
    public Recipe Recipe { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Url => $"/api/recipes/{RecipeId}/image/{Id}";
}