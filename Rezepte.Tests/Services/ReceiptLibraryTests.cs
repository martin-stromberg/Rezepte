using Rezepte.Models;
using Rezepte.Services;
using Rezepte.Tests.Helper;
using System;
using System.Linq;

namespace Rezepte.Tests.Services
{
    internal class ReceiptLibraryTests: BaseTest
    {

        protected override void Process()
        {
            AddTest($"Library with multiple sources considers all sources",
                    Init,
                    Cleanup,
                    Test_AddSourcesInConstructor_AndRequestsUri_ProcessesAllSources);
            AddTest($"Library with multiple later added sources considers all sources",
                    Init,
                    Cleanup,
                    Test_AddSourcesOutsideOfConstructor_AndRequestsUri_ProcessesAllSources);
            AddTest($"Adding new receipt to library",
                    Init,
                    Cleanup,
                    Test_AddNewReceipt_WithNewReceipt_AddsReceiptToDatabase);
            AddTest($"Adding existing receipt to library raises error",
                    Init,
                    Cleanup,
                    Test_AddNewReceipt_WithExistingReceipt_ThrowsException);
            AddTest($"Updating existing receipt in library",
                    Init,
                    Cleanup,
                    Test_UpdateReceipt_WithNewReceipt_ThrowsException);
            AddTest($"Updating new receipt in library raises error",
                    Init,
                    Cleanup,
                    Test_UpdateReceipt_WithExistingReceipt_UpdatesReceiptInDatabase);
        }

        private void Test_AddSourcesInConstructor_AndRequestsUri_ProcessesAllSources()
        {
            var database = new DummyDatabase();
            var sources = new IReceiptSource[]
            {
                new DummyReceiptSource(),
                new DummyReceiptSource(),
                new DummyReceiptSource(),
                new DummyReceiptSource(),
                new DummyReceiptSource()
            };
            var library = new ReceiptLibrary(database, sources);
            var uri = "http://Test_AddSourcesInConstructor_AndRequestsUri_ProcessesAllSources.local";
            var receipt = library.CreateFromUri(uri);
            receipt.Wait();
            CheckIsNull(receipt.Result);
            foreach (var source in sources)
            {
                CheckAreEqual(uri, ((DummyReceiptSource)source).LastUri);
            }
        }

        private void Test_AddSourcesOutsideOfConstructor_AndRequestsUri_ProcessesAllSources()
        {
            var database = new DummyDatabase();
            var sources = new IReceiptSource[]
            {
                new DummyReceiptSource()
            };
            var sources2 = new IReceiptSource[]
            {
                new DummyReceiptSource(),
                new DummyReceiptSource(),
                new DummyReceiptSource(),
                new DummyReceiptSource()
            };
            var library = new ReceiptLibrary(database, sources);

            var uri = "http://Test_AddSourcesOutsideOfConstructor_AndRequestsUri_ProcessesAllSources.1.local";
            var receipt = library.CreateFromUri(uri);
            receipt.Wait();
            CheckIsNull(receipt.Result);
            foreach (var source in sources)
                CheckAreEqual(uri, ((DummyReceiptSource)source).LastUri);
            foreach (var source in sources2)
                CheckIsNull(((DummyReceiptSource)source).LastUri);

            uri = "http://Test_AddSourcesOutsideOfConstructor_AndRequestsUri_ProcessesAllSources.2.local";
            library.AddSources(sources2);
            receipt = library.CreateFromUri(uri);
            receipt.Wait();
            CheckIsNull(receipt.Result);
            foreach (var source in sources)
                CheckAreEqual(uri, ((DummyReceiptSource)source).LastUri);
            foreach (var source in sources2)
                CheckAreEqual(uri, ((DummyReceiptSource)source).LastUri);
        }

        private void Test_AddNewReceipt_WithNewReceipt_AddsReceiptToDatabase()
        {
            var database = new DummyDatabase();
            var library = new ReceiptLibrary(database, null);
            var receipt = new Receipt()
            {
                Title = "Rezept.1",
                Ingredients = new ReceiptIngredients() { Quantity = 1, Ingredients = new ReceiptIngredient[0] },
                Instructions = string.Empty
            };
            library.Add(receipt);

            CheckAreEqual((long)1, receipt.Id);
            var actual = database.GetAll<Rezepte.Services.Database.Models.Receipt>().ToArray();
            var expected = new Rezepte.Services.Database.Models.Receipt[]
            {
                new Rezepte.Services.Database.Models.Receipt()
                {
                    Id = 1,
                    Name = "Rezept.1",
                    Quantity = 1,
                    Ingredients = new Rezepte.Services.Database.Models.ReceiptIngredient[0]
                } };
            CheckAreEqual(expected, actual);
        }

        private void Test_AddNewReceipt_WithExistingReceipt_ThrowsException()
        {
            var database = new DummyDatabase();
            var library = new ReceiptLibrary(database, null);
            var receipt = new Receipt()
            {
                Title = "Rezept.1",
                Ingredients = new ReceiptIngredients() { Quantity = 1, Ingredients = new ReceiptIngredient[0] },
                Instructions = string.Empty
            };
            library.Add(receipt);

            receipt = new Receipt()
            {
                Id = 1,
                Title = "Rezept.2",
                Ingredients = new ReceiptIngredients() { Quantity = 1, Ingredients = new ReceiptIngredient[0] },
                Instructions = string.Empty
            };
            CheckThrows<ArgumentException>(() => library.Add(receipt));
        }

        private void Test_UpdateReceipt_WithNewReceipt_ThrowsException()
        {
            var database = new DummyDatabase();
            var library = new ReceiptLibrary(database, null);
            var receipt = new Receipt()
            {
                Title = "Rezept.1",
                Ingredients = new ReceiptIngredients() { Quantity = 1, Ingredients = new ReceiptIngredient[0] },
                Instructions = string.Empty
            };
            CheckThrows<ArgumentException>(() => library.Update(receipt));
        }

        private void Test_UpdateReceipt_WithExistingReceipt_UpdatesReceiptInDatabase()
        {
            var database = new DummyDatabase();
            var library = new ReceiptLibrary(database, null);
            var receipt = new Receipt()
            {
                Title = "Rezept.1",
                Ingredients = new ReceiptIngredients() { Quantity = 1, Ingredients = new ReceiptIngredient[0] },
                Instructions = string.Empty
            };
            library.Add(receipt);

            receipt = new Receipt()
            {
                Id = 1,
                Title = "Rezept.2",
                Ingredients = new ReceiptIngredients() { Quantity = 1, Ingredients = new ReceiptIngredient[0] },
                Instructions = string.Empty
            };
            library.Update(receipt);

            var actual = database.GetAll<Rezepte.Services.Database.Models.Receipt>().ToArray();
            var expected = new Rezepte.Services.Database.Models.Receipt[]
            {
                new Rezepte.Services.Database.Models.Receipt()
                {
                    Id = 1,
                    Name = "Rezept.2",
                    Quantity = 1,
                    Ingredients = new Rezepte.Services.Database.Models.ReceiptIngredient[0]
                } };
            CheckAreEqual(expected, actual);
        }

    }
}
