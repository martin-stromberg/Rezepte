using Rezepte.Models;

namespace Rezepte.Services.AppToApp
{
    public class ReceiptExport
    {
        public Receipt Receipt { get; set; }
        public ReceiptIngredients Ingredients { get; set; }
    }
}
