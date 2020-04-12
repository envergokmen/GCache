using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCache.Nodes
{
    public interface IGCacheNodeClient
    {
        string Id { get; }
        T Get<T>(string key) where T : class;
        void Set<T>(string key, T data, string fromNode = null);
        void Set<T>(string key, T data, DateTime expireDate, string[] fileDependencies = null, string fromNode = null);
        void Remove(string key, string fromNode = null);
        void ClearAllCache(string fromNode = null);
        List<string> GetAllKeys();
        void RemoveKeyStartsWith(string pattern, string fromNode = null);
    }
}
