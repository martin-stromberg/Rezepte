using SQLite;
using System;
using System.Linq;

namespace Rezepte.Services.Database.Models
{
    public class Receipt: BaseDataModel
    {

        public string Title { get; set; }

        public int Quantity { get; set; }

        public string Instructions { get; set; }

        [Ignore]
        public ReceiptIngredient[] Ingredients { get; set; }

        [Ignore]
        public ReceiptPicture[] Pictures { get; set; }

        protected override void OnRename(long oldId, long newId)
        {
            base.OnRename(oldId, newId);
            if (Pictures != null)
                foreach (var picture in Pictures)
                    picture.ReceiptId = newId;
            if (Ingredients != null)
                foreach (var ingredient in Ingredients)
                    ingredient.ReceiptId = newId;
        }

    }
}
