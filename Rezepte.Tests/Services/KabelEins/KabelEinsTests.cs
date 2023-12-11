using Rezepte.Services.KabelEins;
using System;
using System.Linq;

namespace Rezepte.Tests.Services.KabelEins
{
    internal class KabelEinsTests: BaseTest
    {

        protected override void Process()
        {
            AddTest("Loading KabelEins Receipt (1)", Init, Cleanup, Test_LoadReceipt_WithoutStepSection);
            AddTest("Loading KabelEins Receipt (2)", Init, Cleanup, Test_LoadReceipt_WithStepSection);
        }

        private void Test_LoadReceipt_WithoutStepSection()
        {
            string uri = "https://www.kabeleins.de/serien/abenteuer-leben/rezepte/smrrebrd-strammer-max-8273";

            KabelEinsRezeptSammlung site = new KabelEinsRezeptSammlung();
            var receipt = site.FromUriAsync(uri);
            receipt.Wait();

            CheckIsNotNull(receipt.Result);
            var modelReceipt = receipt.Result.ToModel();
            CheckIsNotNull(modelReceipt);
            CheckIsNotNull(modelReceipt.Pictures);
            CheckIsFalse(string.IsNullOrWhiteSpace(modelReceipt.Title));
            CheckIsFalse(string.IsNullOrWhiteSpace(modelReceipt.Instructions));
            CheckIsTrue(modelReceipt.Ingredients.Ingredients.Length > 0);
        }

        private void Test_LoadReceipt_WithStepSection()
        {
            string uri = "https://www.kabeleins.de/serien/abenteuer-leben/rezepte/frites-deluxe-333784";

            KabelEinsRezeptSammlung site = new KabelEinsRezeptSammlung();
            var receipt = site.FromUriAsync(uri);
            receipt.Wait();

            CheckIsNotNull(receipt.Result);
            var modelReceipt = receipt.Result.ToModel();
            CheckIsNotNull(modelReceipt);
            CheckIsNotNull(modelReceipt.Pictures);
            CheckIsFalse(string.IsNullOrWhiteSpace(modelReceipt.Title));
            CheckIsFalse(string.IsNullOrWhiteSpace(modelReceipt.Instructions));
            CheckIsTrue(modelReceipt.Ingredients.Ingredients.Length > 0);
        }

    }
}
