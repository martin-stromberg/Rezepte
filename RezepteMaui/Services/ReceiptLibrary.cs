using Rezepte.Models;
using Rezepte.Services.Database;
using Rezepte.Services.PictureStorage;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Rezepte.Services
{

    public class ReceiptLibrary
    {

        private ConcurrentBag<IReceiptSource> _Sources = new ConcurrentBag<IReceiptSource>();
        private readonly ICockingDatabase _Database;
        private readonly IPictureStorage _PictureSource;

        public ReceiptLibrary(ICockingDatabase database, IPictureStorage pictureSource, IReceiptSource[] sources)
        {
            _PictureSource = pictureSource;
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
            SavePictures(receipt);

            var dataItem = receipt.ToDataModel() as Database.Models.Receipt;
            _Database.AddOrUpdate(dataItem);

            receipt.Id = dataItem.Id;
        }
        public void Add(ReceiptCollection receiptCollection)
        {
            if (receiptCollection.Id != 0)
                throw new ArgumentException($"Existing receipt cannot be added.");
            
            var dataItem = receiptCollection.ToDataModel() as Database.Models.ReceiptCollection;
            _Database.AddOrUpdate(dataItem);

            receiptCollection.Id = dataItem.Id;
        }
        private void SavePictures(Receipt receipt)
        {
            if (receipt.Pictures != null)
                foreach (var picture in receipt.Pictures)
                    SavePicture(receipt, picture);
        }

        private void SavePicture(Receipt receipt, byte[] picture)
        {
            StringBuilder sb = new StringBuilder();
            using (HashAlgorithm algorithm = SHA256.Create())
                foreach (byte b in algorithm.ComputeHash(picture))
                    sb.Append(b.ToString("X2"));
            var hashValue = sb.ToString();

            if (_PictureSource.Exists(hashValue))
                return;
            var existingImage = _PictureSource.Add(hashValue, picture);
            receipt.PictureHashes = new string[] { hashValue }.Concat(receipt.PictureHashes ?? new string[0]).ToArray();
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

        public IEnumerable<ReceiptCollection> GetCollections()
        {
            return _Database.GetAll<Database.Models.ReceiptCollection>()
                .Select(record => BaseModel.CreateFromDataModel(record) as ReceiptCollection);
        }

        internal Task<ReceiptCollection> CreateCollectionFromName(string name)
        {            
            var existing = GetCollections().FirstOrDefault(coll => coll.Name == name);
            if (existing != null)
                throw new ApplicationException("Already exists.");
            ReceiptCollection receiptCollection = new ReceiptCollection()
            {
                Name = name
            };
            return Task.FromResult(receiptCollection);
        }
    }
}
