using Rezepte.Services.Chefkoch;
using System;
using System.Linq;

namespace Rezepte.Tests.Services.Chefkoch
{
    internal class ChefkochSiteTests: BaseTest
    {

        protected override void Process()
        {
            AddTest($"Load chefkoch receipt", Init, Cleanup, LoadReceipt);
        }

        private void LoadReceipt()
        {
            string uri = $"https://www.chefkoch.de/rezepte/241691097658254/Thunfisch-Sandwich.html";
            ChefkochSite site = new ChefkochSite();
            var receipt = site.LoadReceipt(uri);
            receipt.Wait();

            CheckIsNotNull(receipt.Result);
            CheckIsFalse(string.IsNullOrWhiteSpace(receipt.Result.Title));
            CheckIsFalse(string.IsNullOrWhiteSpace(receipt.Result.Instructions));
            CheckIsTrue(receipt.Result.Ingredients.Quantity > 0);
            CheckIsTrue(receipt.Result.Ingredients.Items.Length > 0);
        }

    }
}
