using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhoneApp1
{
    class AnthuanUtils
    {
        public static async void Post(string address, string parameters, Action<string> onResponseGot)
        {
            Uri uri = new Uri(address);
            HttpClient client = new HttpClient();
            
            try
            {
                await client.PostAsync(uri, new StringContent(parameters));

                var response = await client.GetStringAsync(uri);
                onResponseGot(response);
            }
            catch (Exception erm)
            {
                Console.WriteLine(erm.Message);
                onResponseGot(null);
            }
        }

        public static async void Post(string address, string token, string parameters, Action<string> onResponseGot)
        {
            try
            {
                Uri uri = new Uri(address);
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", token);

                await client.PostAsync(uri, new StringContent(parameters));

                var response = await client.GetStringAsync(uri);
                if (onResponseGot != null)
                    onResponseGot(response);
            }
            catch (Exception erma)
            {
                Console.WriteLine("foobar");
                onResponseGot(null);
            }
        }

        public static async void Get(string address, string token, Action<string> onResponseGot)
        {
            Uri uri = new Uri(address);
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", token);

            try
            {
                var response = await client.GetStringAsync(uri);
                onResponseGot(response);
            }
            catch
            {
                onResponseGot(null);
            }

        }

        public static async Task WriteFile(string fileName, string text)
        {
            IStorageFolder applicationFolder = ApplicationData.Current.LocalFolder;

            IStorageFile storageFile = await applicationFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            using (Stream stream = await storageFile.OpenStreamForWriteAsync())
            {
                byte[] content = Encoding.UTF8.GetBytes(text);
                await stream.WriteAsync(content, 0, content.Length);
                stream.Close();
            }
        }

        public async static Task<bool> DeleteFile(string fileName)
        {
            try
            {
                IStorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                IStorageFile storageFile = await applicationFolder.GetFileAsync(fileName);
                await storageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async static Task<bool> FileExists(string fileName)
        {
            try
            {
                IStorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                IStorageFile storageFile = await applicationFolder.GetFileAsync(fileName);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static async Task<string> ReadFile(string fileName)
        {
            string text;
            IStorageFolder applicationFolder = ApplicationData.Current.LocalFolder;

            IStorageFile storageFile = await applicationFolder.GetFileAsync(fileName);

            IRandomAccessStream accessStream = await storageFile.OpenReadAsync();

            using (Stream stream = accessStream.AsStreamForRead((int)accessStream.Size))
            {
                byte[] content = new byte[stream.Length];
                await stream.ReadAsync(content, 0, (int)stream.Length);

                text = Encoding.UTF8.GetString(content, 0, content.Length);
            }

            return text;
        }
    }
}
