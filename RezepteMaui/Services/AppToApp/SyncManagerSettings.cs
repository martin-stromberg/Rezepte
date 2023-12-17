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
        private static SyncManagerSettings instance = null;
        public static async void LoadInstance()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("syncManager.json");
                using var reader = new StreamReader(stream);
                var contents = reader.ReadToEnd();
                instance = JsonConvert.DeserializeObject<SyncManagerSettings>(contents);
            }
            catch
            {
                instance = null;
            }
        }
        public static SyncManagerSettings Instance => instance;

        public string Host { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
    }
}
