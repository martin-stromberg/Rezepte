using Rezepte.Services.Chefkoch;
using Rezepte.Tests.Services.Chefkoch.Models;
using System;
using System.Linq;

namespace Rezepte.Tests.Services.Chefkoch
{
    internal class ChefkochSiteTests: BaseTest
    {

        public ChefkochSiteTests()
            : base(new ChefkochReceiptTests()) { }

        protected override void Process()
        {
            AddTest($"Load chefkoch receipt", Init, Cleanup, LoadReceipt);
            AddTest($"Load chefkoch receipt with ingredient categories",
                    Init,
                    Cleanup,
                    LoadReceipt_WithIngredientCategories);
            AddTest($"Load chefkoch receipt from wrong page.",
                    Init,
                    Cleanup,
                    LoadReceipt_FromNonReceiptPage_ReturnsNull);
        }

        private void LoadReceipt()
        {
            string uri = $"https://www.chefkoch.de/rezepte/241691097658254/Thunfisch-Sandwich.html";

            ChefkochSite site = new ChefkochSite();
            var receipt = site.LoadReceipt(uri);
            receipt.Wait();

            CheckIsNotNull(receipt.Result);
            CheckIsNotNull(receipt.Result.Pictures);
            CheckIsFalse(string.IsNullOrWhiteSpace(receipt.Result.Title));
            CheckIsFalse(string.IsNullOrWhiteSpace(receipt.Result.Instructions));
            CheckIsTrue(receipt.Result.Ingredients.Quantity > 0);
            CheckIsTrue(receipt.Result.Ingredients.Items.Length > 0);
        }

        private void LoadReceipt_WithIngredientCategories()
        {
            string uri = $"https://www.chefkoch.de/rezepte/1045091209458542/Pizza-Regina.html";

            ChefkochSite site = new ChefkochSite();
            var receipt = site.LoadReceipt(uri);
            receipt.Wait();

            CheckIsNotNull(receipt.Result);
            CheckIsNotNull(receipt.Result.Pictures);
            CheckIsFalse(string.IsNullOrWhiteSpace(receipt.Result.Title));
            CheckIsFalse(string.IsNullOrWhiteSpace(receipt.Result.Instructions));
            CheckIsTrue(receipt.Result.Ingredients.Quantity > 0);
            CheckIsTrue(receipt.Result.Ingredients.Items.Length > 0);
        }

        private void LoadReceipt_FromNonReceiptPage_ReturnsNull()
        {
            string[] uris = new string[]
            {
                "https://www.chefkoch.de/",
                "https://www.chefkoch.de/rezepte/was-backe-ich-heute/",
                "https://www.chefkoch.de/magazin/artikel/14840/Chefkoch/gut-organisiert-der-perfekte-vorrat-rezepte.html#recipe-slider-14836_1"
            };

            ChefkochSite site = new ChefkochSite();
            foreach (var uri in uris)
            {
                var receipt = site.LoadReceipt(uri);
                receipt.Wait();
                CheckIsNull(receipt.Result);
            }
        }

    }
}
