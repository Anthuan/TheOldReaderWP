using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhoneApp1
{
    class AnthuanUtils
    {
        public async static void Post(string address, string parameters, Action<string> onResponseGot)
        {
            Uri uri = new Uri(address);
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(uri);
            r.Method = "POST";

            try
            {
                r.BeginGetRequestStream(delegate(IAsyncResult req)
                {
                    var outStream = r.EndGetRequestStream(req);

                    using (StreamWriter w = new StreamWriter(outStream))
                        w.Write(parameters);

                    r.BeginGetResponse(delegate(IAsyncResult result)
                    {
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);

                            using (var stream = response.GetResponseStream())
                            {
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    onResponseGot(reader.ReadToEnd());
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            onResponseGot(null);
                        }

                    }, null);

                }, null);
            }
            catch (Exception erm)
            {
                Console.WriteLine(erm.Message);
            }
        }

        public static void Post(string address, string token, string parameters, Action<string> onResponseGot)
        {
            try
            {
                Uri uri = new Uri(address);
                HttpWebRequest r = (HttpWebRequest)WebRequest.Create(uri);
                r.Method = "POST";
                r.Headers["Authorization"] = token;

                r.BeginGetRequestStream(delegate(IAsyncResult req)
                {
                    var outStream = r.EndGetRequestStream(req);

                    using (StreamWriter w = new StreamWriter(outStream))
                        w.Write(parameters);

                    r.BeginGetResponse(delegate(IAsyncResult result)
                    {
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);

                            using (var stream = response.GetResponseStream())
                            {
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    if (onResponseGot != null)
                                    {
                                        onResponseGot(reader.ReadToEnd());
                                    }
                                    else
                                    {
                                        reader.ReadToEnd();
                                        reader.Close();
                                    }
                                }
                            }
                        }
                        catch
                        {
                            onResponseGot(null);
                        }

                    }, null);

                }, null);
            }
            catch (Exception erma)
            {
                Console.WriteLine("foobar");
            }
        }

        public static void Get(string address, string token, Action<string> onResponseGot)
        {
            Uri uri = new Uri(address);
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(uri);
            r.Method = "GET";
            r.Headers["Authorization"] = token;

            r.BeginGetResponse(delegate(IAsyncResult result)
            {
                try
                {
                    HttpWebResponse response = (HttpWebResponse)r.EndGetResponse(result);

                    using (var stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            onResponseGot(reader.ReadToEnd());
                        }
                    }
                }
                catch
                {
                    onResponseGot(null);
                }

            }, null);
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
