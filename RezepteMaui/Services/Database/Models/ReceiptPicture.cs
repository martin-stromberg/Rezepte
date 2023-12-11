namespace Rezepte.Services.Database.Models
{

    public class ReceiptPicture: BaseDataModel
    {

        [ForeignKey(ParentType = typeof(Receipt))]
        public long ReceiptId { get; set; }

        public string HashValue { get; set; }

    }
}
