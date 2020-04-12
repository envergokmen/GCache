using GCache.Nodes;
using GCache.PersistentProviders;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCache
{
    public class G2CacheService : GCache.Caching.CachingBase
    {
        private static object lockObj = new object();
        private static Dictionary<string, CacheVM> cache = new Dictionary<string, CacheVM>();
        private DateTime lastAllocationDate = DateTime.UtcNow;
        IPersistentProvider persister = new PersistentFactory().Persister;
        string Id = "1"; // ConfigurationManager.AppSettings["id"];
        INodeSyncronizer nodeSynconizer = new NodeSyncronizer();

        public override Task<CacheVM> GetCache(CacheVM request, ServerCallContext context)
        {
            if (!String.IsNullOrWhiteSpace(request.Key))
            {

                CacheVM cachedContent = null;
                cache.TryGetValue(request.Key, out cachedContent);

                if (cachedContent != null && cachedContent.ExpiresAt.ToDateTime() < DateTime.UtcNow)
                {
                    lock (lockObj)
                    {
                        cache.Remove(cachedContent.Key);
                    }

                    Task.Run(() => AllocateMemory());
                }

                //for persistent mode
                if (cachedContent == null)
                {
                    cachedContent = persister.TryToGetFromPersistent(request.Key);
                    if (cachedContent != null)
                    {
                        AddToMemoryCache(cachedContent);
                    }
                }

                return Task.FromResult(cachedContent);
            }

            return Task.FromResult(new CacheVM {  Succeed=false });
        }
        
        public override Task<CacheVM> SetCache(CacheVM request, ServerCallContext context)
        {
            var result = GenerateResultObject(request);

            try
            {
                if (request.Source != null && request.Source == this.Id) return Task.FromResult(result);

                if (request != null && !String.IsNullOrWhiteSpace(request.Key))
                {
                    AddToMemoryCache(request);

                    Task.Run(() => persister.AddToPersistentCache(request));
                    Task.Run(() => nodeSynconizer.AddToNodes(request, request.Source));
                    Task.Run(() => AllocateMemory());
                }
            }
            catch (Exception)
            {
                result.Succeed = false;
                return Task.FromResult(result);
            }

            return Task.FromResult(result);

        }


        public override Task<CacheVM> RemoveCacheStartsWith(CacheVM request, ServerCallContext context)
        {
            var result = GenerateResultObject(request);

            if (request.Source == this.Id) return Task.FromResult(result);

            if (!String.IsNullOrWhiteSpace(request.Key))
            {
                lock (lockObj)
                {
                    foreach (var item in cache.ToList())
                    {
                        if (item.Key.StartsWith(request.Key))
                        {
                            cache.Remove(item.Key);
                        }
                    }

                }

                Task.Run(() => AllocateMemory());
                Task.Run(() => nodeSynconizer.DeleteFromNodesStartsWith(request, request.Source));
                persister.DeleteCacheBulk(request.Key);
            }

            return Task.FromResult(result);
        }


        public override Task<CacheVM> RemoveCache(CacheVM request, ServerCallContext context)
        {

            var result = GenerateResultObject(request);

            if (request.Source == this.Id) return Task.FromResult(result);

            if (request != null && !String.IsNullOrWhiteSpace(request.Key))
            {
                cache.Remove(request.Key);
                persister.DeleteCache(request.Key);
                nodeSynconizer.DeleteFromNodes(request, request.Source);
            }

            return Task.FromResult(result);

        }

        public override Task<KeyResponse> GetKeys(CacheVM request, ServerCallContext context)
        {
            var result = new KeyResponse();

            foreach (var item in cache)
            {
                result.Keys.Add(item.Key);
            }

            return Task.FromResult(result);
        }

        public override Task<CacheVM> Flush(CacheVM request, ServerCallContext context)
        {
            return RemoveAllCache(request, context);
        }

        public override Task<AllItemsReponse> GetAll(CacheVM request, ServerCallContext context)
        {
            var result = new AllItemsReponse();

            foreach (var item in cache)
            {
                result.Items.Add(item.Value);
            }

            return Task.FromResult(result);
        }


        public override Task<CacheVM> RemoveAllCache(CacheVM request, ServerCallContext context)
        {
            var result = GenerateResultObject(request);
            if (request.Source == this.Id) return Task.FromResult(result);

            lock (lockObj)
            {
                cache = new Dictionary<string, CacheVM>();
            }

            Task.Run(() => GC.Collect());
            persister.DeleteCacheBulk();
            Task.Run(() => nodeSynconizer.DeleteFromAllNodes(request, request.Source));

            return Task.FromResult(result);
        }

        private static CacheVM GenerateResultObject(CacheVM request)
        {
            return new CacheVM
            {
                ExpiresAt = request.ExpiresAt,
                Key = request.Key,
                Succeed = true
            };
        }

        /// <summary>
        /// Moves all keys to new dictionary in order to free memory
        /// </summary>
        /// <param name="context">Current HttpListener Context</param>
        private void AllocateMemory()
        {
            if (lastAllocationDate > DateTime.Now.AddMinutes(-30))
            {
                return;
            }

            lock (lockObj)
            {
                var cache2 = new Dictionary<string, CacheVM>();

                foreach (var item in cache)
                {
                    if (item.Value.ExpiresAt.ToDateTime() > DateTime.UtcNow)
                    {
                        cache2.Add(item.Key, item.Value);
                    }
                }

                cache = cache2;
                GC.Collect();
                lastAllocationDate = DateTime.Now;

            }


        }

        private void AddToMemoryCache(CacheVM cacheForTheSet)
        {
            lock (lockObj)
            {
                if (cache.ContainsKey(cacheForTheSet.Key))
                {
                    cache.Remove(cacheForTheSet.Key);
                };

                cache.Add(cacheForTheSet.Key, cacheForTheSet);
            }
        }


    }
}
