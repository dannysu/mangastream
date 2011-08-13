using System;
using System.Text;
using System.IO;
using System.IO.IsolatedStorage;

namespace MangaStreamCommon
{
    public class Storage
    {
        public static string ReadFileToString(string filePath)
        {
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream stream = null;
            string data;
            try
            {
                stream = store.OpenFile(filePath, FileMode.Open);

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            return data;
        }

        public static void SaveStreamToFile(Stream stream, string cachePath)
        {
            IsolatedStorageFileStream fileStream = null;
            try
            {
                byte[] buffer = new byte[stream.Length];

                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                if (!store.FileExists(cachePath))
                {
                    fileStream = store.CreateFile(cachePath);

                    stream.Read(buffer, 0, buffer.Length);
                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }
    }
}
