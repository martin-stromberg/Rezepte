using Rezepte.Models;
using System;
using System.Linq;

namespace Rezepte.ViewModels
{
    public class ReceiptListItemViewModel: BaseViewModel
    {

        public ReceiptListItemViewModel(Receipt item)
        {
            Item = item;
        }

        public Receipt Item
        {
            get
            {
                return GetProperty<Receipt>();
            }
            private set
            {
                SetProperty<Receipt>(value);
                Name = value?.Title;
            }
        }

        public string Name
        {
            get
            {
                return GetProperty<string>();
            }
            set
            {
                SetProperty<string>(value);
            }
        }

    }
}
