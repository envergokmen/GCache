using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCache.ViewModels;

namespace GCache.PersistentProviders
{
    public class NoPersistentProvider : IPersistentProvider
    {
        public bool PersistentMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void AddToPersistentCache(CacheVM item)
        {
            return;
        }

        public void DeleteCache(string key)
        {
            return;
        }

        public void DeleteCacheBulk(string startsWith = null)
        {
            return;
        }

        public Dictionary<string, CacheVM> GetAllCachedItems()
        {
            return new Dictionary<string, CacheVM>();
        }

        public CacheVM TryToGetFromPersistent(string key)
        {
            return null;
        }
    }
}
