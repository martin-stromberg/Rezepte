namespace Rezepte.Services.Database.Models
{
    public class ReceiptIngredient: BaseDataModel
    {

        public long ReceiptId { get; set; }

        public string Quantity { get; set; }

        public string Name { get; set; }

    }

}
