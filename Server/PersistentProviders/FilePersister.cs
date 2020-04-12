using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using GCache.ViewModels;

namespace GCache.PersistentProviders
{
    public class FilePersister : IPersistentProvider
    {
        private static object lockObj = new object();
        private string CacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CachedFiles");

        public FilePersister()
        {
            if (!Directory.Exists(this.CacheFolder)) { Directory.CreateDirectory(this.CacheFolder); }
        }

        public void AddToPersistentCache(CacheVM item)
        {
            try
            {

                lock (lockObj)
                {
                    File.WriteAllText(Path.Combine(this.CacheFolder, item.Key + ".txt"), JsonConvert.SerializeObject(item), Encoding.UTF8);
                }

            }
            finally
            {


            }
        }

        public void DeleteCacheBulk(string startsWith = null)
        {
            try
            {
                var files = new DirectoryInfo(this.CacheFolder).GetFiles();
                if (startsWith != null) files = files.Where(c => c.Name.StartsWith(startsWith)).ToArray();

                lock (lockObj)
                {
                    foreach (var fileItem in files)
                    {
                        var cachedFile = fileItem.FullName;

                        if (File.Exists(cachedFile))
                        {

                            File.Delete(cachedFile);
                        }

                    }
                }

            }
            finally
            {

            }
        }

        public Dictionary<string, CacheVM> GetAllCachedItems()
        {
            Dictionary<string, CacheVM> cachedItems = new Dictionary<string, CacheVM>();

            try
            {
                var files = new DirectoryInfo(this.CacheFolder).GetFiles();

                foreach (var fileItem in files)
                {
                    var cachedFile = fileItem.FullName;

                    if (File.Exists(cachedFile))
                    {
                        var cachedFileItem = JsonConvert.DeserializeObject<CacheVM>(File.ReadAllText(cachedFile, Encoding.UTF8));

                        if (cachedFileItem != null)
                        {
                            if (cachedFileItem.ExpiresAt.ToDateTime() < DateTime.UtcNow)
                            {
                                lock (this)
                                {
                                    File.Delete(cachedFile);
                                }
                            }
                            else
                            {
                                cachedItems.Add(cachedFileItem.Key, cachedFileItem);
                            }
                        }
                    }

                }
            }
            finally
            {

            }

            return cachedItems;
        }

        public void DeleteCache(string key)
        {
            try
            {
                var cachedFile = Path.Combine(this.CacheFolder, key + ".txt");
                if (File.Exists(cachedFile))
                {
                    lock (lockObj)
                    {
                        File.Delete(cachedFile);
                    }
                }

            }
            finally
            {

            }
        }

        public CacheVM TryToGetFromPersistent(string key)
        {
            CacheVM cachedFileItem = null;

            try
            {

                var cachedFile = Path.Combine(this.CacheFolder, key + ".txt");
                if (File.Exists(cachedFile))
                {
                    cachedFileItem = JsonConvert.DeserializeObject<CacheVM>(File.ReadAllText(cachedFile, Encoding.UTF8));

                    if (cachedFileItem != null)
                    {
                        if (cachedFileItem.ExpiresAt.ToDateTime() < DateTime.UtcNow)
                        {
                            lock (lockObj)
                            {
                                File.Delete(cachedFile);
                            }
                        }
                        else
                        {
                            //AddToMemoryCache(cachedFileItem);

                        }
                    }
                }
            }
            finally
            {

            }

            return cachedFileItem;
        }


    }
}
