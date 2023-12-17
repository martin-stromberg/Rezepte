using Rezepte.Models;

namespace Rezepte.Services.AppToApp
{
    public class ReceiptCollectionExport
    {

        public ReceiptCollection Collection { get; set; }
        public long[] ReceiptIds { get; set; }

        internal static ReceiptCollectionExport FromCollection(ReceiptCollection collection)
        {
            return new ReceiptCollectionExport()
            {
                Collection = collection
            };
        }
    }
}
