using Rezepte.Services.Database.Models;

namespace Rezepte.Models
{
    [DataModelReference(typeof(Services.Database.Models.ReceiptIngredient))]
    public class ReceiptIngredient: BaseModel
    {

        public string Quantity { get; set; }

        public string Name { get; set; }

    }
}
