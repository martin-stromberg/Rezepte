using Rezepte.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezepte.Services.History
{
    public class ReceiptHistory
    {
        private readonly ReceiptLibrary receiptLibrary;

        public ReceiptHistory(ReceiptLibrary receiptLibrary)
        {
            this.receiptLibrary = receiptLibrary;
            this.receiptLibrary.ReceiptAdded += ReceiptLibrary_ReceiptAdded;
        }


       public int MaxCount { get; set; } = 10;

        private void ReceiptLibrary_ReceiptAdded(object sender, BaseModelEventArgs e)
        {
            Initialize();
            Add(e.Item as Receipt);
        }


        public event EventHandler<BaseModelEventArgs> ReceiptAdded;
        public event EventHandler<BaseModelEventArgs> ReceiptRemoved;
        //ToDo Event für Hinzugef+ügtes und Gelöschtes!
        private void Add(Receipt receipt, bool isInitLoad = false)
        {
            receipts.Insert(0, receipt);
            ReceiptAdded?.Invoke(this, new BaseModelEventArgs(receipt));

            while (receipts.Count > MaxCount)
            {
                receipt = receipts[receipts.Count - 1];
                receipts.Remove(receipt);
                ReceiptRemoved?.Invoke(this, new BaseModelEventArgs(receipt));
            }
            if (!isInitLoad)
                UpdateDatabase();
        }

        private void UpdateDatabase()
        {
            var records = receiptLibrary.GetLatestReceipts().ToArray();
            var newRecords = receipts
                .OrderBy(x => x.Id)
                .Select(r => new LatestReceipt()
                {
                    Id = 0,
                    ReceiptId = r.Id
                })
                .ToArray();
            for (int idx = 0; idx < records.Count(); idx++)
            {
                newRecords[idx].CreatedAt = records[idx].CreatedAt;
                newRecords[idx].Id = records[idx].Id;
            }
            foreach (var rec in newRecords)
            {
                if (rec.Id == 0)
                    receiptLibrary.Add(rec);
                else
                    receiptLibrary.Update(rec);
            }
        }

        public void Initialize()
        {
            if (receipts != null)
                return;
            receipts = new ObservableCollection<Receipt>();
            LoadHistory();
        }

        private void LoadHistory()
        {
            foreach (var receipt in receiptLibrary.GetLatestReceipts()
                .Select(record => receiptLibrary.GetReceipt(record.ReceiptId))
                .Where(receipt => receipt is not null))
                Add(receipt, true);
            if (receipts.Count == 0)
                foreach (var receipt in receiptLibrary.GetRange(0, int.MaxValue))
                    Add(receipt, true);
            UpdateDatabase();
        }

        private ObservableCollection<Receipt> receipts = null;
    }
}
