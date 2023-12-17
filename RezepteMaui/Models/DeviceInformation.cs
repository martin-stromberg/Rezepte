using Rezepte.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezepte.Models
{
    [DataModelReference(typeof(Rezepte.Services.Database.Models.DeviceIdentification))]
    public class DeviceInformation: BaseModel
    {
        public long DeviceId { get; set; }
    }
}
