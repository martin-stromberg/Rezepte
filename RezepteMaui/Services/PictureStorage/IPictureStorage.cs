namespace Rezepte.Services.PictureStorage
{
    public interface IPictureStorage
    {

        object Get(string hashValue);

        object Add(string hashValue, byte[] data);

        bool Exists(string hashValue);

    }
}
