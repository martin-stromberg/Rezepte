namespace Rezepte.Services.Database.Models
{
    public class ReceiptCollectionEntry : BaseDataModel
    {
        public long CollectionId { get; set; }
        public long ReceiptId { get; set;}
    }
}
