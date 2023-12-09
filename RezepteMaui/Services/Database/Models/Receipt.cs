using SQLite;
using System;
using System.Linq;

namespace Rezepte.Services.Database.Models
{
    public class Receipt: BaseDataModel
    {

        public string Name { get; set; }

        public int Quantity { get; set; }

        [Ignore]
        public ReceiptIngredient[] Ingredients { get; set; }

    }

}
