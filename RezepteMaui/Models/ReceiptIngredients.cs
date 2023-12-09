namespace Rezepte.Models
{
    public class ReceiptIngredients: BaseModel
    {

        public int Quantity { get; set; }

        public ReceiptIngredient[] Ingredients { get; set; }

    }
}
