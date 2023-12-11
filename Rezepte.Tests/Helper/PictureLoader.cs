using System;
using System.Linq;
using System.Reflection;

namespace Rezepte.Tests.Helper
{
    internal class PictureLoader
    {

        public static byte[] LoadFirstImage()
        {
            var assembly = Assembly.GetEntryAssembly();
            var assemblyName = assembly.GetName().Name;
            var tempFilePath = Path.GetTempFileName();
            using (Stream input = assembly.GetManifestResourceStream($"Rezepte.Resources.Images.rezepte.png"))
                using (Stream output = File.Create(tempFilePath))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, bytesRead);
                    }
                }
            try
            {
                return File.ReadAllBytes(tempFilePath);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

    }
}
