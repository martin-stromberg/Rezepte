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

        public event EventHandler<BaseModelEventArgs> ReceiptCollectionRemoved;
        protected void OnReceiptCollectionRemoved(BaseModelEventArgs args)
        {
            ReceiptCollectionRemoved?.Invoke(this, args);
        }
        public event EventHandler<BaseModelEventArgs> ReceiptRemoved;
        protected void OnReceiptRemoved(BaseModelEventArgs args)
        {
            ReceiptRemoved?.Invoke(this, args);
        }

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

        public void Update(ReceiptCollection collection)
        {
            if (collection.Id == 0)
                throw new ArgumentException($"New receipt cannot be updated.");
            var dataItem = collection.ToDataModel();
            _Database.AddOrUpdate(dataItem);
            collection.Id = dataItem.Id;
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

        public bool IsInCollection(Receipt receipt, ReceiptCollection collection)
        {
            return _Database
                .GetAll<Database.Models.ReceiptCollectionEntry>()
                .Any(entry => entry.CollectionId == collection.Id && entry.ReceiptId == receipt.Id);
        }

        internal void AddToCollection(Receipt receipt, ReceiptCollection collection)
        {
            var existing = _Database
                .GetAll<Database.Models.ReceiptCollectionEntry>()
                .FirstOrDefault(entry => entry.CollectionId == collection.Id && entry.ReceiptId == receipt.Id);
            if (existing != null)
                return;
            var record = new Database.Models.ReceiptCollectionEntry()
            {
                ReceiptId = receipt.Id,
                CollectionId = collection.Id,
            };
            _Database.Add(record);

            if (string.IsNullOrEmpty(collection.PictureHash) && receipt.PictureHashes?.Length > 0)
            {
                collection.PictureHash = receipt.PictureHashes.FirstOrDefault();
                Update(collection);
            }
        }

        internal void RemoveFromCollection(Receipt receipt, ReceiptCollection collection)
        {
            var existing = _Database
                .GetAll<Database.Models.ReceiptCollectionEntry>()
                .FirstOrDefault(entry => entry.CollectionId == collection.Id && entry.ReceiptId == receipt.Id);
            if (existing == null)
                return;
            _Database.Remove(existing);
            if (receipt.PictureHashes != null && receipt.PictureHashes.Contains(collection.PictureHash))
            {                
                var firstReceipt = GetRange(0, int.MaxValue).FirstOrDefault(receipt => IsInCollection(receipt, collection) && receipt.PictureHashes != null && receipt.PictureHashes.Length > 0);
                collection.PictureHash = firstReceipt?.PictureHashes.FirstOrDefault();
                Update(collection);
            }
        }

        internal void RemoveCollection(ReceiptCollection collection)
        {
            var existing = _Database.Get<Database.Models.ReceiptCollection>(collection.Id);
            if (existing == null)
                return;
            var receipts = GetRange(collection, 0, int.MaxValue);
            foreach (var receipt in receipts)
                RemoveFromCollection(receipt, collection);
            _Database.Remove(existing);
            OnReceiptCollectionRemoved(new BaseModelEventArgs(collection));
        }

        internal void RemoveReceipt(Receipt item)
        {
            var existing = _Database.Get<Database.Models.Receipt>(item.Id);
            if (existing == null)
                return;
            var collections = GetCollections();
            foreach (var collection in collections)
                if (IsInCollection(item, collection))
                    RemoveFromCollection(item, collection);
            _Database.Remove(existing);
            OnReceiptRemoved(new BaseModelEventArgs(item));
        }

        internal IEnumerable<Receipt> GetRange(ReceiptCollection collection, int offset, int count)
        {
            return _Database.GetAll<Database.Models.ReceiptCollectionEntry>()
                .Where(record => record.CollectionId == collection.Id)
                .Skip(offset)
                .Take(count)
                .Select(record => GetReceipt(record.ReceiptId));
        }

        private Receipt GetReceipt(long receiptId)
        {
            var record = _Database.Get<Database.Models.Receipt>(receiptId);
            if (record == null)
                return null;
            return BaseModel.CreateFromDataModel(record) as Receipt;
        }

        internal Receipt FindReceiptByUri(string receiptUri)
        {
            var record = _Database.GetAll<Database.Models.Receipt>().FirstOrDefault(receipt => receipt.Uri?.ToLower() == receiptUri.ToLower());
            if (record == null) return null;
            return BaseModel.CreateFromDataModel(record) as Receipt;
        }

        internal async Task<Receipt[]> CreateReceipts(string html)
        {
            if (html.StartsWith("http://") || html.StartsWith("https://"))
            {
                var existing = FindReceiptByUri(html);
                if (existing == null)
                    existing = await CreateFromUri(html);
                return new Receipt[] { existing }
                    .Where(receipt => receipt != null)
                    .ToArray();
            }

            List<Receipt> receiptList = new List<Receipt>();
            foreach (var source in _Sources)         
            {
                var uris = await source.ExtractUris(html);
                if (uris != null && uris.Any())
                    foreach (var uri in uris)
                    {
                        var existing = FindReceiptByUri(uri);
                        var receipt = existing ?? await CreateFromUri(uri);
                        if (receipt == null)
                            continue;
                        receiptList.Add(receipt);
                    }
            }
            return receiptList.ToArray();
        }

        
    }
}
