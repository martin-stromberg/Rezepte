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
        }

        private void Test_AddSourcesInConstructor_AndRequestsUri_ProcessesAllSources()
        {
            var sources = new IReceiptSource[]
            {
                new DummyReceiptSource(),
                new DummyReceiptSource(),
                new DummyReceiptSource(),
                new DummyReceiptSource(),
                new DummyReceiptSource()
            };
            var library = new ReceiptLibrary(sources);
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
            var library = new ReceiptLibrary(sources);

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

    }
}
