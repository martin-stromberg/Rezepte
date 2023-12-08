using Rezepte.Services.Database;
using Rezepte.Services.Database.Models;
using System;
using System.Linq;

namespace Rezepte.Tests.Services.Database
{
    internal class CockingDatabaseTests: BaseTest
    {

        private string rootFolder = string.Empty;

        protected string RootFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(rootFolder))
                {
                    string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    folder = Path.Combine(folder, "Rezepte", "Tests");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    rootFolder = folder;
                }
                return rootFolder;
            }
        }

        protected string DatabaseFilePath
        {
            get
            {
                return Path.Combine(RootFolder, "RezepteTests.db3");
            }
        }

        public override void Init()
        {
            base.Init();
            Cleanup(true);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            Cleanup(false);
        }

        private void Cleanup(bool isInitializationCleanup)
        {
            if (File.Exists(DatabaseFilePath))
                File.Delete(DatabaseFilePath);
            CheckIsFalse(File.Exists(DatabaseFilePath), $"Could not clean up test database.");
            if (!isInitializationCleanup)
            {
                if (Directory.Exists(RootFolder))
                    Directory.Delete(RootFolder, true);
                CheckIsFalse(Directory.Exists(RootFolder), $"Could not clean up test directory.");
            }
            if (isInitializationCleanup)
                rootFolder = string.Empty;
        }

        protected override void Process()
        {
            AddTest($"Creating a new database", Init, Cleanup, TestCreateDatabase);
            AddTest($"Adding a new receipt", Init, Cleanup, TestAddReceipt_WithNewReceipt_AddsReceipt);
            AddTest($"Adding a receipt with defined primary key throws exception",
                    Init,
                    Cleanup,
                    TestAddReceipt_WithIdNotZero_ThrowsException);
            AddTest($"Updating a receipt with undefined primary key throws exception",
                    Init,
                    Cleanup,
                    TestUpdateReceipt_WithIdZero_ThrowsException);
            AddTest($"Updating a receipt", Init, Cleanup, TestUpdateReceipt_ForExistingReceipt_UpdatesRecord);
        }

        private void TestCreateDatabase()
        {
            CockingDatabaseSettings Settings = new CockingDatabaseSettings() { FilePath = DatabaseFilePath };
            using (CockingDatabase Database = new CockingDatabase(Settings))
                Database.Open();
            CheckIsTrue(File.Exists(Settings.FilePath), $"Test database file is not present.");
        }

        private void TestAddReceipt_WithNewReceipt_AddsReceipt()
        {
            CockingDatabaseSettings Settings = new CockingDatabaseSettings() { FilePath = DatabaseFilePath };
            using (CockingDatabase Database = new CockingDatabase(Settings))
            {
                var expected = new Receipt() { Name = "TestAddReceipt" };

                Database.Open();
                Database.Add(expected);

                expected.Id = 1;
                var actual = Database.GetAll<Receipt>();
                CheckAreEqual(1, actual.Count(), $"Record count does not match expectation");
                CheckAreEqual(expected, actual.FirstOrDefault());
            }
        }

        private void TestAddReceipt_WithIdNotZero_ThrowsException()
        {
            CockingDatabaseSettings Settings = new CockingDatabaseSettings() { FilePath = DatabaseFilePath };
            using (CockingDatabase Database = new CockingDatabase(Settings))
            {
                var expected = new Receipt() { Id = 1, Name = "TestAddReceipt" };

                Database.Open();
                CheckThrows<ArgumentException>(() => { Database.Add(expected); });

                var actual = Database.GetAll<Receipt>();
                CheckAreEqual(0, actual.Count(), $"Record count does not match expectation");
            }
        }

        private void TestUpdateReceipt_WithIdZero_ThrowsException()
        {
            CockingDatabaseSettings Settings = new CockingDatabaseSettings() { FilePath = DatabaseFilePath };
            using (CockingDatabase Database = new CockingDatabase(Settings))
            {
                var expected = new Receipt() { Id = 0, Name = "TestUpdateReceipt_WithIdZero_ThrowsException" };

                Database.Open();
                CheckThrows<ArgumentException>(() => { Database.Update(expected); });

                var actual = Database.GetAll<Receipt>();
                CheckAreEqual(0, actual.Count(), $"Record count does not match expectation");
            }
        }

        private void TestUpdateReceipt_ForExistingReceipt_UpdatesRecord()
        {
            CockingDatabaseSettings Settings = new CockingDatabaseSettings() { FilePath = DatabaseFilePath };
            using (CockingDatabase Database = new CockingDatabase(Settings))
            {
                var expected = new Receipt() { Id = 0, Name = "TestUpdateReceipt_WithIdZero_ThrowsException" };

                Database.Open();
                Database.Add(expected);

                expected.Name = $"{expected.Name}_New";
                Database.Update(expected);

                var actual = Database.Get<Receipt>(expected.Id);
                CheckAreEqual(expected, actual);
            }
        }

    }
}
