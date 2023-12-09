using Rezepte.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Rezepte.Services
{
    public class ReceiptLibrary
    {

        private ConcurrentBag<IReceiptSource> _sources = new ConcurrentBag<IReceiptSource>();

        public ReceiptLibrary(IReceiptSource[] sources)
        {
            AddSources(sources);
        }

        public void AddSources(params IReceiptSource[] sources)
        {
            foreach (var source in sources)
                AddSource(source);
        }

        private void AddSource(IReceiptSource source)
        {
            _sources.Add(source);
        }

        public async Task<Receipt> CreateFromUri(string uri)
        {
            foreach (var source in _sources)
            {
                var receipt = await source.FromUriAsync(uri);
                if (receipt != null)
                    return receipt.ToModel();
            }
            return null;
        }

    }
}
