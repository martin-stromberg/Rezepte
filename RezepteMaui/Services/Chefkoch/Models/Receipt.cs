using System;
using System.Linq;

namespace Rezepte.Services.Chefkoch.Models
{
    public class Receipt
    {

        public string Title { get; internal set; }

        public ReceiptIngredients Ingredients { get; internal set; }

        public string Instructions { get; set; }

    }

    public class ReceiptIngredients
    {

        public ReceiptIngredient[] Items { get; internal set; }

        public int Quantity { get; set; }

    }

    public class ReceiptIngredient
    {

        private string quantity;

        public string Quantity
        {
            get
            {
                return quantity;
            }
            set
            {
                while (value.Contains("  "))
                    value = value.Replace("  ", " ");
                quantity = value;
            }
        }

        private string name;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                while (value.Contains("  "))
                    value = value.Replace("  ", " ");
                name = value;
            }
        }

    }
}
