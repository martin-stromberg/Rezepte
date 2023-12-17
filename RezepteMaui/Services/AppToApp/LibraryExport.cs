namespace Rezepte.Services.AppToApp
{
    public class LibraryExport
    {
        private ReceiptExport[] receipts;

        public string[] PictureHashes { get; set; }
        public DateTime CreatedAt { get; set; }
        public ReceiptCollectionExport[] Collections { get; set; }
        public ReceiptExport[] Receipts 
        { 
            get => receipts; 
            set 
            { 
                receipts = value;
                PictureHashes = value
                    .SelectMany(receipt => receipt.Receipt.PictureHashes)
                    .ToArray();
            } 
        }

        public long DeviceId { get; set; }
    }
}
