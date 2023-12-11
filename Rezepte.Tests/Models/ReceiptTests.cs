using Rezepte.Models;
using System;
using System.Linq;

namespace Rezepte.Tests.Models
{
    internal class ReceiptTests: BaseTest
    {

        protected override void Process()
        {
            AddTest($"Create receipt model from data model object.", Init, Cleanup, Test_CreateFromDataModel);
        }

        private void Test_CreateFromDataModel()
        {
            var record = new Rezepte.Services.Database.Models.Receipt()
            {
                Id = 1,
                Title = "Test",
                Quantity = 1,
                Instructions = "Alles zusammenrühren",
                Ingredients = new[]
                   {
                       new Rezepte.Services.Database.Models.ReceiptIngredient
                       {
                           Id = 1,
                           Name = "Test",
                           Quantity = "1",
                           ReceiptId = 1
                       },
                       new Rezepte.Services.Database.Models.ReceiptIngredient
                       {
                           Id = 2,
                           Name = "Test.2",
                           Quantity = "2",
                           ReceiptId = 1
                       } },
                Pictures = new[]
                {
                    new Rezepte.Services.Database.Models.ReceiptPicture { Id = 1, ReceiptId = 1, HashValue = "123456" } }
            };
            var actual = Receipt.CreateFromDataModel(record);
            var expected = new Receipt()
            {
                Id = 1,
                Title = "Test",
                Ingredients = new ReceiptIngredients()
                {
                    Id = 0,
                    Quantity = 1,
                    Ingredients = new ReceiptIngredient[]
                    {
                        new ReceiptIngredient() { Id = 1, Name = "Test", Quantity = "1" },
                         new ReceiptIngredient() { Id = 2, Name = "Test.2", Quantity = "2" } }
                },
                Instructions = "Alles zusammenrühren",
                PictureHashes = new[] { "123456" },
                Pictures = null
            };
            CheckAreEqual(expected, actual);
        }

    }
}
