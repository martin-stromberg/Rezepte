using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Rezepte.Services.Ftp;
using Microsoft.Maui.Controls;
using System.Text.Json.Serialization;
using Rezepte.Models;
using Newtonsoft.Json;
using Rezepte.Services.PictureStorage;

namespace Rezepte.Services.AppToApp
{
    public class SyncManager
    {
        private readonly ReceiptLibrary receiptLibrary;
        private readonly IPictureStorage pictureStorage;
        private readonly SyncManagerSettings settings;
        private const string remotePath = "/htdocs/rezepte";

        public SyncManager(ReceiptLibrary receiptLibrary, IPictureStorage picturestorage, SyncManagerSettings settings)
        {
            this.receiptLibrary = receiptLibrary;
            this.pictureStorage = picturestorage;
            this.settings = settings;
        }

        private FtpShare CreateConnection() {
            try
            {
                FtpShare share = new FtpShare(settings.Host, settings.User, settings.Pass);
                share.Connect();
                return share;
            }
            catch
            {
                return null;
            }
        }
        private bool syncing = false;
        public void Sync(bool justInport = true)
        {
            if (syncing)
                return;
            syncing = true;
            try
            {
                LibraryExport import = LoadExportedPackage();
                SyncLibrary(import);

                if (!justInport)
                {
                    LibraryExport export = CreateExportPackage();
                    SaveExport(export);
                }
            }
            catch
            {

            }
            finally
            {
                syncing = false;
            }
        }

        private void SaveExport(LibraryExport export)
        {
            var tempFilePath = Path.GetTempFileName();
            var share = CreateConnection();
            try
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                File.WriteAllText(tempFilePath, JsonConvert.SerializeObject(export));

                foreach (var hash in export.PictureHashes)
                {
                    var filePath = pictureStorage.GetFilePath(hash);
                    if (File.Exists(filePath))
                    {
                        var remoteFilePath = $"{remotePath}/pictures/{Path.GetFileName(filePath)}";
                        if (!share.FileExists(remoteFilePath))
                            share.UploadFile(remoteFilePath, filePath);
                    }
                }
                share.UploadFile($"{remotePath}/export.dat", tempFilePath, FluentFTP.FtpRemoteExists.Overwrite);
            }
            catch
            {
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                share.Disconnect();
            }
        }

        private void SyncLibrary(LibraryExport import)
        {
            if (import == null)
                return;
            var device = receiptLibrary.GetDevice();
            if (import.DeviceId == device.DeviceId)
                return;
            LoadPictures(import.PictureHashes);
            ImportCollections(import.Collections);
            ImportReceipts(import.Receipts, import.Collections);
        }

        private void LoadPictures(string[] pictureHashes)
        {
            var share = CreateConnection();
            try
            {
                foreach (var hash in pictureHashes
                    .Where(hash => !pictureStorage.Exists(hash)))
                {
                    var localFilePath = pictureStorage.GetFilePath(hash);
                    var remoteFilePath = $"{remotePath}/pictures/{Path.GetFileName(localFilePath)}";                    
                    share.DownloadFile(remoteFilePath, localFilePath);
                }
            }
            catch
            {
            }
            finally
            {
                share.Disconnect();
            }
        }

        private void ImportCollections(ReceiptCollectionExport[] collections)
        {
            foreach (var collection in collections)
                ImportCollection(collection);
        }

        private void ImportCollection(ReceiptCollectionExport collection)
        {
            var existing = receiptLibrary.GetCollections().FirstOrDefault(col => col.Name == collection.Collection.Name);
            if (existing != null)
            {
                collection.Collection.Id = existing.Id;
                return;
            }
            collection.Collection.Id = 0;
            receiptLibrary.Add(collection.Collection);
        }

        private void ImportReceipts(ReceiptExport[] receipts, ReceiptCollectionExport[] collections)
        {
            foreach (var receipt in receipts)
                ImportReceipts(receipt, collections);
        }

        private void ImportReceipts(ReceiptExport receiptExport, ReceiptCollectionExport[] collections)
        {
            Receipt existingReceipt = null;
            Receipt receipt = receiptExport.Receipt;
            receipt.Ingredients = receiptExport.Ingredients;            
            long oldReceiptId = receipt.Id;
            try
            {
                existingReceipt = receiptLibrary.FindReceiptByUri(receipt.Uri);
                if (existingReceipt != null)
                {
                    receipt.Id = existingReceipt.Id;                    
                    return;
                }
                if (string.IsNullOrWhiteSpace(receipt.Uri))
                {
                    existingReceipt = receiptLibrary.FindReceiptByTitle(receipt.Title);
                    if (existingReceipt != null)
                    {
                        receipt.Id = existingReceipt.Id;
                        return;
                    }
                }
                existingReceipt = receipt;
                existingReceipt.Id = 0;
                foreach (var ingredient in receipt.Ingredients.Ingredients)
                    ingredient.Id = 0;
                receiptLibrary.Add(existingReceipt);
            }
            catch(Exception ex)
            {
                existingReceipt = null;
                throw;
            }
            finally
            {
                if (existingReceipt != null)
                    SyncReceiptCollections(existingReceipt, collections, oldReceiptId);
            }
        }

        private void SyncReceiptCollections(Receipt receipt, ReceiptCollectionExport[] collections, long oldReceiptId)
        {
            collections = collections
                .Where(col => col.ReceiptIds.Contains(oldReceiptId))
                .ToArray();
            var existingCollections = receiptLibrary
                .GetCollections()
                .Where(col => receiptLibrary.IsInCollection(receipt, col))
                .ToArray();
            var collectionsToRemove = existingCollections
                .Where(col => !collections.Select(col => col.Collection).Any(c => c.Id == col.Id))
                .ToArray();
            var collectionsToAdd = collections
                .Select(col => col.Collection)
                .Where(col => !existingCollections.Any(c => c.Id == col.Id))
                .ToArray();
            foreach (var collection in collectionsToRemove)
                receiptLibrary.RemoveFromCollection(receipt, collection);
            foreach (var collection in collectionsToAdd)
                receiptLibrary.AddToCollection(receipt, collection);
        }

        private void ChangeReceiptId(long id1, long id2, ReceiptCollectionExport[] collections)
        {
            collections = collections.Where(col => col.ReceiptIds.Contains(id1)).ToArray();
            foreach (var collection in collections)
                collection.ReceiptIds = collection.ReceiptIds.Where(id => id != id1).Concat(new long[] { id2 }).ToArray();
        }

        private LibraryExport LoadExportedPackage()
        {
            var tempFilePath = Path.GetTempFileName();
            var share = CreateConnection();
            try
            {
                share.DownloadFile($"{remotePath}/export.dat", tempFilePath);
                return JsonConvert.DeserializeObject<LibraryExport>(File.ReadAllText(tempFilePath));
            }
            catch
            {
                return null;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                share.Disconnect();
            }
        }

        private LibraryExport CreateExportPackage()
        {
            var device = receiptLibrary.GetDevice();
            LibraryExport export = new LibraryExport()
            {
                CreatedAt = DateTime.Now,
                DeviceId = device.DeviceId,
                Receipts = receiptLibrary
                    .GetRange(0, int.MaxValue)
                    .Select(r =>
                    {
                        return new ReceiptExport()
                        {
                            Receipt = r,
                            Ingredients = r.Ingredients
                        };
                    })
                    .ToArray(),
                Collections = receiptLibrary
                    .GetCollections()
                    .Select(collection => ReceiptCollectionExport.FromCollection(collection))
                    .Select(collection =>
                    {
                        collection.ReceiptIds = receiptLibrary
                            .GetRange(collection.Collection, 0, int.MaxValue)
                            .Select(receipt => receipt.Id)
                            .ToArray();
                        return collection;
                    })
                    .ToArray()
            };
            return export;
        }

    }
}
