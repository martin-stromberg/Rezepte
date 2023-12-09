using Rezepte.Services;
using System;
using System.Linq;

namespace Rezepte.Tests.Helper
{
    internal class DummyReceiptSource: IReceiptSource
    {

        public Task<ISourceReceipt> FromUriAsync(string uri)
        {
            LastUri = uri;
            return Task.FromResult(ReceiptToReturn);
        }

        public ISourceReceipt ReceiptToReturn { get; set; }

        public string LastUri { get; set; }

    }
}
