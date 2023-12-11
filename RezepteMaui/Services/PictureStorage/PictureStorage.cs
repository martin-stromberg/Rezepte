namespace Rezepte.Services.PictureStorage
{
    public class PictureStorage: IPictureStorage
    {

        private readonly IPictureStorageSettings _Settings;

        public PictureStorage(IPictureStorageSettings settings)
        {
            _Settings = settings;
        }

        public object Add(string hashValue, byte[] data)
        {
            string FilePath = Path.Combine(_Settings.RootPath, $"{hashValue}.dat");
            File.WriteAllBytes(FilePath, data);
            return data;
        }

        public bool Exists(string hashValue)
        {
            string FilePath = Path.Combine(_Settings.RootPath, $"{hashValue}.dat");
            return File.Exists(FilePath);
        }

        public object Get(string hashValue)
        {
            string FilePath = Path.Combine(_Settings.RootPath, $"{hashValue}.dat");
            if (!File.Exists(FilePath))
                return null;
            return File.ReadAllBytes(FilePath);
        }

    }
}
