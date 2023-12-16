using System;
using System.Linq;

namespace Rezepte.Services.KabelEins.Models
{
    public class Receipt: ISourceReceipt
    {

        public string Title { get; set; }

        public ReceiptIngredients Ingredients { get; set; }

        public string Instructions { get; set; }

        public byte[][] Pictures { get; set; }
        public string URI { get; set; }

        public Rezepte.Models.Receipt ToModel()
        {
            return new Rezepte.Models.Receipt()
            {
                Title = Title,
                Ingredients = Ingredients.ToModel(),
                Instructions = Instructions,
                Pictures = Pictures,
                Uri = URI
            };
        }

    }

    public class ReceiptIngredients
    {

        public ReceiptIngredient[] Items { get; set; }

        public int Quantity { get; set; }

        public Rezepte.Models.ReceiptIngredients ToModel()
        {
            return new Rezepte.Models.ReceiptIngredients()
            {
                Quantity = Quantity,
                Ingredients = Items.Select(item => item.ToModel()).ToArray()
            };
        }

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

        public Rezepte.Models.ReceiptIngredient ToModel()
        {
            return new Rezepte.Models.ReceiptIngredient() { Quantity = Quantity, Name = Name };
        }

    }
}
