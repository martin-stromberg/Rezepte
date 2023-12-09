using System;
using System.Linq;

namespace Rezepte.Models
{
    public class Receipt: BaseModel
    {

        public Receipt() { }

        public string Title { get; set; }

        public string Instructions { get; set; }

        public ReceiptIngredients Ingredients { get; set; }

    }

    public class ReceiptIngredients: BaseModel
    {

        public int Quantity { get; set; }

        public ReceiptIngredient[] Ingredients { get; set; }

    }

    public class ReceiptIngredient: BaseModel
    {

        public string Quantity { get; set; }

        public string Name { get; set; }

    }
}
