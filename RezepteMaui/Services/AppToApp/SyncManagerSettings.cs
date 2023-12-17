using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rezepte.Services.AppToApp
{
    public class SyncManagerSettings
    {
        public string Host { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }

        internal static async Task<SyncManagerSettings> LoadAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("syncManager.json");
                using var reader = new StreamReader(stream);
                var contents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<SyncManagerSettings>(contents);
            }
            catch 
            { 
                return null; 
            }
        }
    }
}
