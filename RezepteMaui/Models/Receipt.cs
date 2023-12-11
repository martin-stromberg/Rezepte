using Rezepte.Services.Database.Models;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Rezepte.Models
{
    [DataModelReference(typeof(Rezepte.Services.Database.Models.Receipt))]
    public class Receipt: BaseModel
    {

        public Receipt() { }

        public string Title { get; set; }

        public string Instructions { get; set; }

        [IgnoreDataMember]
        public ReceiptIngredients Ingredients { get; set; }

        [IgnoreDataMember]
        public byte[][] Pictures { get; set; }

        public string[] PictureHashes { get; set; }

        public override BaseDataModel ToDataModel()
        {
            var obj = base.ToDataModel() as Rezepte.Services.Database.Models.Receipt;
            obj.Title = Title;
            obj.Quantity = Ingredients.Quantity;
            obj.Ingredients = Ingredients.Ingredients
                                         .Select(i => i.ToDataModel())
                                         .Cast<Rezepte.Services.Database.Models.ReceiptIngredient>()
                                         .Select(i =>
                                         {
                                             i.ReceiptId = Id;
                                             return i;
                                         })
                                         .ToArray();
            obj.Pictures = PictureHashes?.Select(hash => new ReceiptPicture() { HashValue = hash, ReceiptId = Id })
                                         .ToArray();
            return obj;
        }

        protected override void Update(BaseDataModel record)
        {
            base.Update(record);
            var receiptRecord = record as Rezepte.Services.Database.Models.Receipt;
            Title = receiptRecord?.Title;
            Ingredients = new ReceiptIngredients()
            {
                Id = Id,
                Quantity = receiptRecord.Quantity,
                Ingredients = receiptRecord.Ingredients?.Select(ing =>
                                                                new ReceiptIngredient()
                    {
                        Id = ing.Id,
                        Name = ing.Name,
                        Quantity = ing.Quantity
                    })
                                                        .ToArray()
            };
            PictureHashes = receiptRecord.Pictures?.Select(pic => pic.HashValue).ToArray();
        }

    }
}
