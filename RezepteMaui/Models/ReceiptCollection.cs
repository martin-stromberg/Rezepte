using Rezepte.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezepte.Models
{
    [DataModelReference(typeof(Rezepte.Services.Database.Models.ReceiptCollection))]
    public class ReceiptCollection: BaseModel
    {
        public string Name { get; set; }
        public string PictureHash { get; set; }
        public int Order { get; set; }
    }
}
