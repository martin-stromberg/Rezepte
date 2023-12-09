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

        public override BaseDataModel ToDataModel()
        {
            var obj = base.ToDataModel() as Rezepte.Services.Database.Models.Receipt;
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
            return obj;
        }

    }
}
