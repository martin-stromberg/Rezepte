using Rezepte.Models;
using Rezepte.Services.Database;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Rezepte.Services
{
    public class ReceiptLibrary
    {

        private ConcurrentBag<IReceiptSource> _Sources = new ConcurrentBag<IReceiptSource>();
        private readonly ICockingDatabase _Database;

        public ReceiptLibrary(ICockingDatabase database, IReceiptSource[] sources)
        {
            _Database = database;
            if (sources != null)
                AddSources(sources);
        }

        public void AddSources(params IReceiptSource[] sources)
        {
            foreach (var source in sources)
                AddSource(source);
        }

        private void AddSource(IReceiptSource source)
        {
            _Sources.Add(source);
        }

        public async Task<Receipt> CreateFromUri(string uri)
        {
            foreach (var source in _Sources)
            {
                var receipt = await source.FromUriAsync(uri);
                if (receipt != null)
                    return receipt.ToModel();
            }
            return null;
        }

        public void Add(Receipt receipt)
        {
            if (receipt.Id != 0)
                throw new ArgumentException($"Existing receipt cannot be added.");
            var dataItem = receipt.ToDataModel() as Database.Models.Receipt;
            _Database.AddOrUpdate(dataItem);
            receipt.Id = dataItem.Id;
        }

        public void Update(Receipt receipt)
        {
            if (receipt.Id == 0)
                throw new ArgumentException($"New receipt cannot be updated.");
            var dataItem = receipt.ToDataModel();
            _Database.AddOrUpdate(dataItem);
            receipt.Id = dataItem.Id;
        }

        public IEnumerable<Receipt> GetRange(int offset, int count)
        {
            return _Database.GetAll<Database.Models.Receipt>()
                            .Skip(offset)
                            .Take(count)
                            .Select(record => BaseModel.CreateFromDataModel(record) as Receipt);
        }

    }
}
