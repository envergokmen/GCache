using GCache.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCache.PersistentProviders
{
    public interface IPersistentProvider
    {
        void AddToPersistentCache(CacheVM item);
        CacheVM TryToGetFromPersistent(string key);
        Dictionary<string, CacheVM> GetAllCachedItems();
        void DeleteCacheBulk(string startsWith = null);
        void DeleteCache(string key);

    }
}
