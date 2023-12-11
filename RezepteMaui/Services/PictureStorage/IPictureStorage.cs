namespace Rezepte.Services.PictureStorage
{
    public interface IPictureStorage
    {

        ImageSource Get(string hashValue);

        object Add(string hashValue, byte[] data);

        bool Exists(string hashValue);

    }
}
