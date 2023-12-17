using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezepte.Services.Database.Models
{
    public class ReceiptCollection : BaseDataModel
    {
        public string Name { get; set; }

        public string PictureHash { get; set; }
        public int Order {  get; set; }
    }
}
