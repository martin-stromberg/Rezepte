using Rezepte.Tests.Helper;
using System;
using System.Linq;

namespace Rezepte.Tests.Services.Chefkoch.Models
{
    internal class ChefkochReceiptTests: BaseTest
    {

        protected override void Process()
        {
            AddTest("Chefkoch.de receipt create correct model object.", Init, Cleanup, Test_ToModel);
        }

        private void Test_ToModel()
        {
            var pictures = new byte[][] { PictureLoader.LoadFirstImage() };
            var receipt = new Rezepte.Services.Chefkoch.Models.Receipt()
            {
                Title = "Test_ToModel",
                Ingredients = new Rezepte.Services.Chefkoch.Models.ReceiptIngredients
                {
                    Quantity = 2,
                    Items = new Rezepte.Services.Chefkoch.Models.ReceiptIngredient[]
                    {
                        new Rezepte.Services.Chefkoch.Models.ReceiptIngredient() { Quantity = "2kg", Name = "Schmalz" },
                        new Rezepte.Services.Chefkoch.Models.ReceiptIngredient() { Quantity = "1kg", Name = "Salz" },
                        new Rezepte.Services.Chefkoch.Models.ReceiptIngredient() { Quantity = "3kg", Name = "Talk" },
                    }
                },
                Instructions = $@"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, 
no sea takimata sanctus est Lorem ipsum dolor sit amet.",
                Pictures = pictures
            };
            var actual = receipt.ToModel();
            var expected = new Rezepte.Models.Receipt()
            {
                Title = "Test_ToModel",
                Instructions = $@"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, 
no sea takimata sanctus est Lorem ipsum dolor sit amet.",
                Ingredients = new Rezepte.Models.ReceiptIngredients()
                {
                    Quantity = 2,
                    Ingredients = new Rezepte.Models.ReceiptIngredient[]
                    {
                        new Rezepte.Models.ReceiptIngredient() { Quantity = "2kg", Name = "Schmalz" },
                        new Rezepte.Models.ReceiptIngredient() { Quantity = "1kg", Name = "Salz" },
                        new Rezepte.Models.ReceiptIngredient() { Quantity = "3kg", Name = "Talk" }
                    }
                },
                Pictures = pictures
            };
            CheckAreEqual(expected, actual);
        }

    }
}
