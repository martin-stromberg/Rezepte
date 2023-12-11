using Rezepte.Models;
using Rezepte.Services;
using Rezepte.Services.PictureStorage;
using Rezepte.Tests.Helper;
using System;
using System.Linq;

namespace Rezepte.Tests.Services
{
    internal class ReceiptLibraryTests: BaseTest
    {

        private IPictureStorageSettings pictureStorageSettings = null;

        protected IPictureStorageSettings PictureStorageSettings
        {
            get
            {
                if (pictureStorageSettings == null)
                {
                    pictureStorageSettings = new PictureStorageSettings()
                    {
                        RootPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Tests")
                    };
                }
                if (!Directory.Exists(pictureStorageSettings.RootPath))
                    Directory.CreateDirectory(pictureStorageSettings.RootPath);
                return pictureStorageSettings;
            }
        }

        public override void Init()
        {
            base.Init();
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if (Directory.Exists(PictureStorageSettings.RootPath))
                Directory.Delete(PictureStorageSettings.RootPath, true);
        }

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
            var pictureSource = new PictureStorage(PictureStorageSettings);
            var library = new ReceiptLibrary(database, pictureSource, sources);
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
            var pictureSource = new PictureStorage(PictureStorageSettings);
            var library = new ReceiptLibrary(database, pictureSource, sources);

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
            var pictures = new byte[][] { PictureLoader.LoadFirstImage() };
            var database = new DummyDatabase();
            var pictureSource = new PictureStorage(PictureStorageSettings);
            var library = new ReceiptLibrary(database, pictureSource, null);
            var receipt = new Receipt()
            {
                Title = "Rezept.1",
                Ingredients = new ReceiptIngredients() { Quantity = 1, Ingredients = new ReceiptIngredient[0] },
                Instructions = string.Empty,
                Pictures = pictures
            };
            library.Add(receipt);

            CheckAreEqual((long)1, receipt.Id);
            var actual = database.GetAll<Rezepte.Services.Database.Models.Receipt>().ToArray();
            var expected = new Rezepte.Services.Database.Models.Receipt[]
            {
                new Rezepte.Services.Database.Models.Receipt()
                {
                    Id = 1,
                    Title = "Rezept.1",
                    Quantity = 1,
                    Ingredients = new Rezepte.Services.Database.Models.ReceiptIngredient[0],
                    Pictures = new Rezepte.Services.Database.Models.ReceiptPicture[]
                    {
                        new Rezepte.Services.Database.Models.ReceiptPicture()
                        {
                            ReceiptId = 1,
                            HashValue = "5D7585B6D0C102329261E920AC4A92ABA72C3DE68CED39623D646726492B48F6",
                            Id = 1
                        } }
                } };

            CheckAreEqual(expected, actual);
            CheckAreEqual(expected[0].Pictures, actual[0].Pictures);
            CheckAreEqual(expected[0].Ingredients, actual[0].Ingredients);
        }

        private void Test_AddNewReceipt_WithExistingReceipt_ThrowsException()
        {
            var database = new DummyDatabase();
            var pictureSource = new PictureStorage(PictureStorageSettings);
            var library = new ReceiptLibrary(database, pictureSource, null);
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
            var pictureSource = new PictureStorage(PictureStorageSettings);
            var library = new ReceiptLibrary(database, pictureSource, null);
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
            var pictureSource = new PictureStorage(PictureStorageSettings);
            var library = new ReceiptLibrary(database, pictureSource, null);
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
                    Title = "Rezept.2",
                    Quantity = 1,
                    Ingredients = new Rezepte.Services.Database.Models.ReceiptIngredient[0]
                } };
            CheckAreEqual(expected, actual);
        }

        public void Test_LoadReceiptFromDatabase()
        {
            var database = new DummyDatabase();
            var pictureSource = new PictureStorage(PictureStorageSettings);
            var receipt = new Rezepte.Services.Database.Models.Receipt() { Title = string.Empty, };

            var library = new ReceiptLibrary(database, pictureSource, null);
            var actual = library.GetRange(0, 1).ToArray();

            CheckIsTrue(actual.Length == 1);
            throw new NotImplementedException();
        }

    }
}
